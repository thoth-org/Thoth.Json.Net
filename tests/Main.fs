module Tests.Main

open Expecto
open Util.Testing

module MoveMeToThothJsonRepo =
    open Thoth.Json.Net


    type RecordC =
        {
            C1: string
            C2: string
            C3: string list
        }

    type UnionB =
        | UnionBc1
        | RecordC of RecordC

    type RecordA =
        {
            Af1: string
            Ab: UnionB
        }


    let incorrectJson = """
    {
    "Ab": [
        "RecordC",
        {
        "C1": "",
        "C2": "",
    """


    let tests : Test =
        testList "Thoth.Json.Decode" [

            testList "Errors" [

                testCase "unit works" <| fun _ ->
                    #if FABLE_COMPILER
                    let expected : Result<RecordA, string> = Error "Given an invalid JSON: Unexpected token m in JSON at position 0"
                    #else
                    let expected : Result<RecordA, string> = Error "Given an invalid JSON: Unexpected end when reading token. Path 'Ab[1]'."
                    #endif

                    incorrectJson
                    |> Decode.Auto.fromString<RecordA>
                    |> equal expected
            ]
        ]



[<EntryPoint>]
let main args =
    testList "All" [
                     MoveMeToThothJsonRepo.tests
                     Decoders.tests
                     Encoders.tests
                     BackAndForth.tests
                     ExtraCoders.tests
                   ]
    |> runTestsWithArgs defaultConfig args
