module Util.Testing

open System.Text.RegularExpressions
open Expecto

let testList name tests = testList name tests
let testCase msg test = testCase msg test
let ftestCase msg test = ftestCase msg test

let inline equal expected actual: unit =
    Expect.equal actual expected ""

type Test = Expecto.Test

module String =
    let trim (s: string) = s.Trim()
    let normalizeNewlines (s: string) = Regex(@"\r\n").Replace(s, "\n")

    /// <summary>
    /// Trims and removes all whitespaces.
    /// </summary>
    let normalize = trim >> normalizeNewlines
