module Tests.Main

open Expecto
open Util.Testing

[<EntryPoint>]
let main args =
    testList "All" [ Tests.Decode.tests
                     Tests.Encode.tests
                   ]
    |> runTestsWithArgs defaultConfig args
