#r "paket: groupref netcorebuild //"
#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif

#nowarn "52"

open System
open System.IO
open System.Text.RegularExpressions
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.Tools
open Fake.JavaScript
open Fake.Api

let versionFromGlobalJson : DotNet.CliInstallOptions -> DotNet.CliInstallOptions = (fun o ->
        { o with Version = DotNet.Version (DotNet.getSDKVersionFromGlobalJson()) }
    )

let dotnetSdk = lazy DotNet.install versionFromGlobalJson
let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd

let inline yarnWorkDir (ws : string) (yarnParams : Yarn.YarnParams) =
    { yarnParams with WorkingDirectory = ws }

let root = __SOURCE_DIRECTORY__

module Source =
    let dir = root </> "src"
    let projectFile = dir </> "Thoth.Json.Net.fsproj"
    let paketTemplate = dir </> "paket.template"

module Tests =
    let dir = root </> "tests"
    let projectFile = dir </> "Tests.fsproj"

let gitOwner = "thoth-org"
let repoName = "Thoth.Json.Net"

module Util =

    let visitFile (visitor: string -> string) (fileName : string) =
        File.ReadAllLines(fileName)
        |> Array.map (visitor)
        |> fun lines -> File.WriteAllLines(fileName, lines)

    let replaceLines (replacer: string -> Match -> string option) (reg: Regex) (fileName: string) =
        fileName |> visitFile (fun line ->
            let m = reg.Match(line)
            if not m.Success
            then line
            else
                match replacer line m with
                | None -> line
                | Some newLine -> newLine)

// Module to print colored message in the console
module Logger =
    let consoleColor (fc : ConsoleColor) =
        let current = Console.ForegroundColor
        Console.ForegroundColor <- fc
        { new IDisposable with
              member x.Dispose() = Console.ForegroundColor <- current }

    let warn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.DarkYellow in printf "%s" s) str
    let warnfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.DarkYellow in printfn "%s" s) str
    let error str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Red in printf "%s" s) str
    let errorfn str = Printf.kprintf (fun s -> use c = consoleColor ConsoleColor.Red in printfn "%s" s) str

let run (cmd:string) dir args  =
    Command.RawCommand(cmd, Arguments.OfArgs args)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory dir
    |> Proc.run
    |> ignore

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "tests/**/bin"
    ++ "tests/**/obj"
    ++ "temp/"
    |> Shell.cleanDirs
)

Target.create "YarnInstall"(fun _ ->
    Yarn.install id
)

Target.create "DotnetRestore" (fun _ ->
    DotNet.restore (dtntWorkDir Source.dir) ""
    DotNet.restore (dtntWorkDir Tests.dir) ""
)

let mono workingDir args =
    Command.RawCommand("mono", Arguments.OfArgs args)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> Proc.run
    |> ignore

let build project framework =
    DotNet.build (fun p ->
        { p with Framework = Some framework } ) project

let testNetFrameworkDir = root </> "tests" </> "bin" </> "Release" </> "net461"
let testNetCoreDir = root </> "tests" </> "bin" </> "Release" </> "netcoreapp2.0"

Target.create "AdaptTest" (fun _ ->
    [ "Types.fs"
      "Decoders.fs"
      "Encoders.fs" ]
    |> List.map (fun fileName ->
         root </> "paket-files" </> "thoth-org" </> "Thoth.Json" </> "tests" </> fileName
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

Target.create "Test" (fun _ ->
    build Tests.projectFile "netcoreapp2.0"
    build Tests.projectFile "net461"

    if Environment.isUnix then
        mono testNetFrameworkDir [ "Tests.exe" ]
    else
        run (testNetFrameworkDir </> "Tests.exe") root []

    let result = DotNet.exec (dtntWorkDir testNetCoreDir) "" "Tests.dll"

    if not result.OK then failwithf "Expecto for netcore tests failed."
)

let needsPublishing (versionRegex: Regex) (newVersion: string) projFile =
    printfn "Project: %s" projFile
    if newVersion.ToUpper().EndsWith("NEXT")
        || newVersion.ToUpper().EndsWith("UNRELEASED")
    then
        Logger.warnfn "Version marked as unreleased version in Changelog, don't publish yet."
        false
    else
        File.ReadLines(projFile)
        |> Seq.tryPick (fun line ->
            let m = versionRegex.Match(line)
            if m.Success then Some m else None)
        |> function
            | None -> failwith "Couldn't find version in project file"
            | Some m ->
                let sameVersion = m.Groups.[1].Value = newVersion
                if sameVersion then
                    Logger.warnfn "Already version %s, no need to publish." newVersion
                not sameVersion

let pushNuget (newVersion: string) (projFile: string) =
    let versionRegex = Regex("^version\\s(.*?)$", RegexOptions.IgnoreCase)

    let nugetKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"

    let needsPublishing = needsPublishing versionRegex newVersion projFile

    (versionRegex, projFile) ||> Util.replaceLines (fun line _ ->
        versionRegex.Replace(line, "version " + newVersion) |> Some)

    Paket.pack (fun p ->
        { p with BuildConfig = "Release"
                 Version = newVersion
                 Symbols = true } )

    let files =
        Directory.GetFiles(root </> "temp", "*.nupkg")
        |> Array.find (fun nupkg -> nupkg.Contains(newVersion))
        |> fun x -> [x]

    if needsPublishing then
        Paket.pushFiles (fun o ->
            { o with ApiKey = nugetKey
                     PublishUrl = "https://www.nuget.org/api/v2/package"
                     WorkingDir = __SOURCE_DIRECTORY__ })
            files

let versionRegex = Regex("^## ?\\[?v?([\\w\\d.-]+\\.[\\w\\d.-]+[a-zA-Z0-9])\\]?", RegexOptions.IgnoreCase)

let getLastVersion () =
    File.ReadLines("CHANGELOG.md")
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

let getNotes (version : string) =
    File.ReadLines("CHANGELOG.md")
    |> Seq.skipWhile(fun line ->
        let m = versionRegex.Match(line)

        if m.Success then
            not (m.Groups.[1].Value = version)
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

Target.create "Publish" (fun _ ->
    let version = getLastVersion()
    pushNuget version Source.paketTemplate
)

Target.create "Release" (fun _ ->
    let version = getLastVersion()

    match Git.Information.getBranchName root with
    | "master" ->
        Git.Staging.stageAll root
        let commitMsg = sprintf "Release version %s" version
        Git.Commit.exec root commitMsg
        Git.Branches.push root

        let token =
            match Environment.environVarOrDefault "GITHUB_TOKEN" "" with
            | s when not (System.String.IsNullOrWhiteSpace s) -> s
            | _ -> failwith "The Github token must be set in a GITHUB_TOKEN environmental variable"

        GitHub.createClientWithToken token
        |> GitHub.draftNewRelease gitOwner repoName version (isPreRelease version) (getNotes version)
        |> GitHub.publishDraft
        |> Async.RunSynchronously

    | _ -> failwith "You need to be on the master branch in order to create a Github Release"

)

"Clean"
    ==> "YarnInstall"
    ==> "DotnetRestore"
    ==> "AdaptTest"
    ==> "Test"
    ==> "Publish"
    ==> "Release"

Target.runOrDefault "Test"
