#r "nuget: Fun.Build, 0.2.9"
#r "nuget: Fake.IO.FileSystem, 5.23.1"
#r "nuget: Fake.Core.Environment, 5.23.1"
#r "nuget: Fake.Tools.Git, 5.23.1"
#r "nuget: Fake.Api.GitHub, 5.23.1"
#r "nuget: SimpleExec, 11.0.0"
#r "nuget: BlackFox.CommandLine, 1.0.0"

open Fun.Build
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open System.IO
open Fake.Core
open Fake.Tools
open Fake.Api
open System.Text.RegularExpressions
open BlackFox.CommandLine

let root = __SOURCE_DIRECTORY__
let gitOwner = "thoth-org"
let repoName = "Thoth.Json.Net"

module Changelog =

    let versionRegex = Regex("^## ?\\[?v?([\\w\\d.-]+\\.[\\w\\d.-]+[a-zA-Z0-9])\\]?", RegexOptions.IgnoreCase)

    let getLastVersion (changelodPath : string) =
        File.ReadLines changelodPath
            |> Seq.tryPick (fun line ->
                let m = versionRegex.Match(line)
                if m.Success then Some m else None)
            |> function
                | None -> failwith "Couldn't find version in changelog file"
                | Some m ->
                    m.Groups.[1].Value

    let isPreRelease (version : string) =
        let regex = Regex(".*(alpha|beta|rc).*", RegexOptions.IgnoreCase)
        regex.IsMatch(version)

    let getNotesForVersion (version : string) =
        File.ReadLines("CHANGELOG.md")
        |> Seq.skipWhile(fun line ->
            let m = versionRegex.Match(line)

            if m.Success then
                (m.Groups.[1].Value <> version)
            else
                true
        )
        // Remove the version line
        |> Seq.skip 1
        // Take all until the next version line
        |> Seq.takeWhile (fun line ->
            let m = versionRegex.Match(line)
            not m.Success
        )

module Stages =

    let clean =
        stage "Clean" {
            run (fun _ ->
                !! "src/**/bin"
                ++ "src/**/obj"
                ++ "tests/**/bin"
                ++ "tests/**/obj"
                ++ "temp/"
                |> Shell.cleanDirs
            )
        }

    let adaptTest =
        stage "AdaptTest" {
            // First install the dependencies with Paket
            run "dotnet paket install"

            // Now that the files are present, we can adapt them
            run (fun _ ->
                [
                    "Types.fs"
                    "Decoders.fs"
                    "Encoders.fs"
                    "BackAndForth.fs"
                    "ExtraCoders.fs"
                ]
                |> List.map (fun fileName ->
                    root </> "paket-files" </> "tests" </> "thoth-org" </> "Thoth.Json" </> "tests" </> fileName
                )
                |> List.iter (fun path ->
                    File.ReadLines path
                    |> Seq.toList
                    |> List.map (fun originalLine ->
                        match originalLine.Trim() with
                        | "open Thoth.Json" -> "open Thoth.Json.Net"
                        // | "open Fable.Core"
                        | "open Fable.Core.JsInterop" -> ""
                        | _ -> originalLine
                    )
                    // This is important to manually concat the lines using `\n` otherwise we end up with `CRLF`
                    // and the tests will fail on Windows
                    |> String.concat "\n"
                    |> File.writeString false path
                )
            )
        }

    let test =

        stage "Test" {
            run "dotnet build tests --configuration Release --framework net6.0"
            run "dotnet build tests --configuration Release --framework net461"

            stage "NetFramework - Unix" {
                whenAny {
                    platformOSX
                    platformLinux
                }
                run "mono ./tests/bin/Release/net461/Tests.exe"
            }

            stage "NetFramework - Windows" {
                whenWindows
                run "tests/bin/Release/net461/Tests.exe"
            }

            run "dotnet tests/bin/Release/net6.0/Tests.dll"
        }


pipeline "Setup" {
    description "Setup the project by (cleaning artefacts, adaptiong the tests files, etc.)"
    workingDir __SOURCE_DIRECTORY__

    Stages.clean
    Stages.adaptTest

    runIfOnlySpecified
}

pipeline "Test" {
    description "Run the tests"
    workingDir __SOURCE_DIRECTORY__

    Stages.clean
    Stages.adaptTest
    Stages.test

    runIfOnlySpecified
}

pipeline "Release" {
    description "Release the project on Nuget and GitHub"
    workingDir __SOURCE_DIRECTORY__

    whenAll {
        branch "main"
        envVar "NUGET_KEY"
        envVar "GITHUB_TOKEN_THOTH_ORG"
    }

    Stages.clean
    Stages.adaptTest
    Stages.test

    stage "Publish packages to NuGet" {
        run "dotnet pack src -c Release"

        run (fun ctx ->
            let nugetKey = ctx.GetEnvVar "NUGET_KEY"
            cmd $"dotnet nuget push src/bin/Release/*.nupkg -s https://api.nuget.org/v3/index.json -k {nugetKey}"
        )
    }

    stage "Release on Github" {
        run (fun ctx ->
            let githubToken = ctx.GetEnvVar "GITHUB_TOKEN_THOTH_ORG"

            let version = Changelog.getLastVersion "CHANGELOG.md"
            let isPreRelease = Changelog.isPreRelease version
            let notes = Changelog.getNotesForVersion version

            Git.Staging.stageAll root
            let commitMsg = $"Release version {version}"
            Git.Commit.exec root commitMsg
            Git.Branches.push root

            GitHub.createClientWithToken githubToken
            |> GitHub.draftNewRelease gitOwner repoName version isPreRelease notes
            |> GitHub.publishDraft
        )
    }

    runIfOnlySpecified
}

tryPrintPipelineCommandHelp ()
