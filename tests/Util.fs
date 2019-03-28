module Util.Testing

open Expecto

let testList name tests = testList name tests
let testCase msg test = testCase msg test

let inline equal expected actual: unit =
    Expect.equal actual expected ""

type Test = Expecto.Test
