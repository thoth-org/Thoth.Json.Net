module Tests.Main

open Expecto
open Util.Testing

[<EntryPoint>]
let main args =
    testList "All" [ Decoders.tests
                     Encoders.tests
                     Converter.tests
                     BackAndForth.tests
                     ExtraCoders.tests
                   ]
    |> runTestsWithArgs defaultConfig args
