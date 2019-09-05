module Tests.Converter

open Newtonsoft.Json
open Expecto
open Util.Testing
open Thoth.Json.Net

let encodeWithConverter<'T> (converter: JsonConverter) (space: int) (value: obj) : string =
    let format = if space = 0 then Formatting.None else Formatting.Indented
    let settings = JsonSerializerSettings(Converters = [|converter|],
                                          Formatting = format,
                                          DateTimeZoneHandling = DateTimeZoneHandling.Utc )
    JsonConvert.SerializeObject(value, settings)

let decodeStringWithConverter<'T> (converter: JsonConverter) (json: string): Result<'T, string> =
    try
        JsonConvert.DeserializeObject<'T>(json, converter) |> Ok
    with ex ->
        Error("Given an invalid JSON: " + ex.Message)


type MyUnion = Foo of int | Bar

type Record2 =
    { a : float
      b : float }

type Record1 = {s : string option}
type Record9 =
    { a: int
      b: string
      c: (bool * int) list
      d: MyUnion
      e: Map<string, Record2>
      f: System.DateTime
      g: float
      h: bool
   // i: System.DayOfWeek
    }

let tests : Test =
    testList "Thoth.Json.Converter" [

        testList "Decoding with Converter" [

            testCase "works with int" <| fun _ ->
               let converter = Thoth.Json.Net.Converters.Converter false
               let json = "42"
               decodeStringWithConverter converter json
               |> equal (Ok 42)
            testCase "works with bool" <| fun _ ->
               let converter = Thoth.Json.Net.Converters.Converter false
               let json = "true"
               decodeStringWithConverter converter json
               |> equal (Ok true)

            testCase "works with float" <| fun _ ->
               let converter = Thoth.Json.Net.Converters.Converter false
               let json = "1.2"
               decodeStringWithConverter converter json
               |> equal (Ok 1.2)
               
            testCase "works with enum" <| fun _ ->
               let converter = Thoth.Json.Net.Converters.Converter false
               let json = "42"
               decodeStringWithConverter converter json
               |> equal (Ok 42)

            testCase "works with string" <| fun _ ->
               let converter = Thoth.Json.Net.Converters.Converter false
               let json = "\"maxime\""
               decodeStringWithConverter converter json
               |> equal (Ok "maxime")

            testCase "with simple Union" <| fun _ ->
               let converter = Thoth.Json.Net.Converters.Converter false
               let json = "2"
               decodeStringWithConverter converter json
               |> equal (Ok System.DayOfWeek.Tuesday)

        ]
        
        testList "Roundtrips" [
            testCase "roundtrip works" <| fun _ ->
                let converter = Thoth.Json.Net.Converters.Converter false
                let json =
                    { a = 5
                      b = "bar"
                      c = [false, 3; true, 5; false, 10]
                      d = Foo 14
                      e = Map [("oh", { a = 2.; b = 2. }); ("ah", { a = -1.5; b = 0. })]
                      f = System.DateTime.Now
                      g = 1.2
                      h = false
                   // i = System.DayOfWeek.Tuesday
                    } |> encodeWithConverter converter 4
                let result = decodeStringWithConverter converter json
                match result with
                | Error er -> failwith er
                | Ok r2 ->
                    equal 5 r2.a
                    equal "bar" r2.b
                    equal [false, 3; true, 5; false, 10] r2.c
                    equal (Foo 14) r2.d
                    equal -1.5 (Map.find "ah" r2.e).a
                    equal 2.   (Map.find "oh" r2.e).b
                    equal 1.2   r2.g
                    equal false r2.h
                    equal System.DayOfWeek.Tuesday r2.i

            testCase "roundtrip with option" <| fun _ ->
                    let converter = Thoth.Json.Net.Converters.Converter false
                    let json =
                        { s = None
                        } |> encodeWithConverter converter 4
                    let result = decodeStringWithConverter converter json
                    match result with
                    | Error er -> failwith er
                    | Ok r ->
                        equal None r.s
                        
            testCase "roundrip with union" <| fun _ ->
                let converter = Thoth.Json.Net.Converters.Converter false
                
                let json = Bar |> encodeWithConverter converter 4  // "Bar"
                let result = decodeStringWithConverter converter json
                match result with
                | Error er -> failwith er
                | Ok r ->
                    equal Bar r
        ] 
    ]
