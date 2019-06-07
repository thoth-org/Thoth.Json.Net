module Tests.Main

open Expecto
open Util.Testing

[<EntryPoint>]
let main args =
    testList "All" [ Tests.Decoders.tests
                     Tests.Encoders.tests
                     Tests.Converter.tests
                   ]
    |> runTestsWithArgs defaultConfig args
