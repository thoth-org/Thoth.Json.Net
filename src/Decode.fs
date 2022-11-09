namespace Thoth.Json.Net

[<RequireQualifiedAccess>]
module Decode =

    open System.Globalization
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open System.IO

    module Helpers =
        let anyToString (token: JsonValue) : string =
            if isNull token then "null"
            else
                use stream = new StringWriter(NewLine = "\n")
                use jsonWriter = new JsonTextWriter(
                                        stream,
                                        Formatting = Formatting.Indented,
                                        Indentation = 4 )

                token.WriteTo(jsonWriter)
                stream.ToString()

        let inline getField (fieldName: string) (token: JsonValue) = token.Item(fieldName)
        let inline isBool (token: JsonValue) = not(isNull token) && token.Type = JTokenType.Boolean
        let inline isNumber (token: JsonValue) = not(isNull token) && (token.Type = JTokenType.Float || token.Type = JTokenType.Integer)
        let inline isIntegralValue (token: JsonValue) = not(isNull token) && (token.Type = JTokenType.Integer)
        let inline isInteger (token: JsonValue) = not(isNull token) && (token.Type = JTokenType.Integer)
        let inline isString (token: JsonValue) = not(isNull token) && token.Type = JTokenType.String
        let inline isGuid (token: JsonValue) = not(isNull token) && token.Type = JTokenType.Guid
        let inline isDate (token: JsonValue) = not(isNull token) && token.Type = JTokenType.Date
        let inline isArray (token: JsonValue) = not(isNull token) && token.Type = JTokenType.Array
        let inline isObject (token: JsonValue) = not(isNull token) && token.Type = JTokenType.Object
        let inline isUndefined (token: JsonValue) = isNull token
        let inline isNullValue (token: JsonValue) = isNull token || token.Type = JTokenType.Null
        let inline asBool (token: JsonValue): bool = token.Value<bool>()
        let inline asInt (token: JsonValue): int = token.Value<int>()
        let inline asFloat (token: JsonValue): float = token.Value<float>()
        let inline asFloat32 (token: JsonValue): float32 = token.Value<float32>()
        let inline asDecimal (token: JsonValue): System.Decimal = token.Value<System.Decimal>()
        let inline asString (token: JsonValue): string = token.Value<string>()
        let inline asArray (token: JsonValue): JsonValue[] = token.Value<JArray>().Values() |> Seq.toArray

    let private genericMsg msg value newLine =
        try
            "Expecting "
                + msg
                + " but instead got:"
                + (if newLine then "\n" else " ")
                + (Helpers.anyToString value)
        with
            | _ ->
                "Expecting "
                + msg
                + " but decoder failed. Couldn't report given value due to circular structure."
                + (if newLine then "\n" else " ")

    let private errorToString (path : string, error) =
        let reason =
            match error with
            | BadPrimitive (msg, value) ->
                genericMsg msg value false
            | BadType (msg, value) ->
                genericMsg msg value true
            | BadPrimitiveExtra (msg, value, reason) ->
                genericMsg msg value false + "\nReason: " + reason
            | BadField (msg, value) ->
                genericMsg msg value true
            | BadPath (msg, value, fieldName) ->
                genericMsg msg value true + ("\nNode `" + fieldName + "` is unkown.")
            | TooSmallArray (msg, value) ->
                "Expecting " + msg + ".\n" + (Helpers.anyToString value)
            | BadOneOf messages ->
                "The following errors were found:\n\n" + String.concat "\n\n" messages
            | FailMessage msg ->
                "The following `failure` occurred with the decoder: " + msg

        match error with
        | BadOneOf _ ->
            // Don't need to show the path here because each error case will show it's own path
            reason
        | _ ->
            "Error at: `" + path + "`\n" + reason

    ///////////////
    // Runners ///
    /////////////

    let fromValue (path : string) (decoder : Decoder<'T>) =
        fun value ->
            match decoder path value with
            | Ok success ->
                Ok success
            | Error error ->
                Error (errorToString error)

    let fromString (decoder : Decoder<'T>) =
        fun value ->
            try
                let serializationSettings =
                    new JsonSerializerSettings(
                        DateParseHandling = DateParseHandling.None,
                        CheckAdditionalContent = true
                    )

                let serializer = JsonSerializer.Create(serializationSettings)

                use reader = new JsonTextReader(new StringReader(value))
                let res = serializer.Deserialize<JToken>(reader)

                fromValue "$" decoder res
            with
                | :? JsonException as ex ->
                    Error("Given an invalid JSON: " + ex.Message)

    let unsafeFromString (decoder : Decoder<'T>) =
        fun value ->
            match fromString decoder value with
            | Ok x -> x
            | Error msg -> failwith msg

    [<System.Obsolete("Please use fromValue instead")>]
    let decodeValue (path : string) (decoder : Decoder<'T>) = fromValue path decoder

    [<System.Obsolete("Please use fromString instead")>]
    let decodeString (decoder : Decoder<'T>) = fromString decoder

    //////////////////
    // Primitives ///
    ////////////////

    let string : Decoder<string> =
        fun path token ->
            if Helpers.isString token then
                Ok(Helpers.asString token)
            else
                (path, BadPrimitive("a string", token)) |> Error

    let guid : Decoder<System.Guid> =
        fun path value ->
            // Using Helpers.isString fails because Json.NET directly assigns Guid type
            if Helpers.isGuid value then
                value.Value<System.Guid>() |> Ok
            elif Helpers.isString value then
                match System.Guid.TryParse (Helpers.asString value) with
                | true, x -> Ok x
                | _ -> (path, BadPrimitive("a guid", value)) |> Error
            else (path, BadPrimitive("a guid", value)) |> Error

    let unit : Decoder<unit> =
        fun path value ->
            if Helpers.isNullValue value then
                Ok ()
            else
                (path, BadPrimitive("null", value)) |> Error

    let inline private integral
                    (name : string)
                    (tryParse : (string -> bool * 'T))
                    (min : unit -> 'T)
                    (max : unit -> 'T)
                    (conv : float -> 'T) : Decoder< 'T > =

        fun path value ->
            if Helpers.isNumber value then
                if Helpers.isIntegralValue value then
                    let fValue = Helpers.asFloat value
                    if (float(min())) <= fValue && fValue <= (float(max())) then
                        Ok(conv fValue)
                    else
                        (path, BadPrimitiveExtra(name, value, "Value was either too large or too small for " + name)) |> Error
                else
                    (path, BadPrimitiveExtra(name, value, "Value is not an integral value")) |> Error
            elif Helpers.isString value then
                match tryParse (Helpers.asString value) with
                | true, x -> Ok x
                | _ -> (path, BadPrimitive(name, value)) |> Error
            else
                (path, BadPrimitive(name, value)) |> Error

    let sbyte : Decoder<sbyte> =
        integral
            "a sbyte"
            System.SByte.TryParse
            (fun () -> System.SByte.MinValue)
            (fun () -> System.SByte.MaxValue)
            sbyte

    /// Alias to Decode.uint8
    let byte : Decoder<byte> =
        integral
            "a byte"
            System.Byte.TryParse
            (fun () -> System.Byte.MinValue)
            (fun () -> System.Byte.MaxValue)
            byte

    let int16 : Decoder<int16> =
        integral
            "an int16"
            System.Int16.TryParse
            (fun () -> System.Int16.MinValue)
            (fun () -> System.Int16.MaxValue)
            int16

    let uint16 : Decoder<uint16> =
        integral
            "an uint16"
            System.UInt16.TryParse
            (fun () -> System.UInt16.MinValue)
            (fun () -> System.UInt16.MaxValue)
            uint16

    let int : Decoder<int> =
        integral
            "an int"
            System.Int32.TryParse
            (fun () -> System.Int32.MinValue)
            (fun () -> System.Int32.MaxValue)
            int

    let uint32 : Decoder<uint32> =
        integral
            "an uint32"
            System.UInt32.TryParse
            (fun () -> System.UInt32.MinValue)
            (fun () -> System.UInt32.MaxValue)
            uint32

    let int64 : Decoder<int64> =
        integral
            "an int64"
            System.Int64.TryParse
            (fun () -> System.Int64.MinValue)
            (fun () -> System.Int64.MaxValue)
            int64

    let uint64 : Decoder<uint64> =
        integral
            "an uint64"
            System.UInt64.TryParse
            (fun () -> System.UInt64.MinValue)
            (fun () -> System.UInt64.MaxValue)
            uint64

    let bigint : Decoder<bigint> =
        fun path token ->
            if Helpers.isNumber token then
                Helpers.asInt token |> bigint |> Ok
            elif Helpers.isString token then
                match bigint.TryParse (Helpers.asString token, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, x -> Ok x
                | _ -> (path, BadPrimitive("a bigint", token)) |> Error
            else
                (path, BadPrimitive("a bigint", token)) |> Error

    let bool : Decoder<bool> =
        fun path token ->
            if Helpers.isBool token then
                Ok(Helpers.asBool token)
            else
                (path, BadPrimitive("a boolean", token)) |> Error

    let float : Decoder<float> =
        fun path token ->
            if Helpers.isNumber token then
                Helpers.asFloat token |> Ok
            else
                (path, BadPrimitive("a float", token)) |> Error

    let float32 : Decoder<float32> =
        fun path token ->
            if Helpers.isNumber token then
                Helpers.asFloat32 token |> Ok
            else
                (path, BadPrimitive("a float", token)) |> Error

    let decimal : Decoder<decimal> =
        fun path token ->
            if Helpers.isNumber token then
                Helpers.asDecimal token |> Ok
            elif Helpers.isString token then
                match System.Decimal.TryParse (Helpers.asString token, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, x -> Ok x
                | _ -> (path, BadPrimitive("a decimal", token)) |> Error
            else
                (path, BadPrimitive("a decimal", token)) |> Error

    [<System.Obsolete("Please use datetimeUtc instead.")>]
    let datetime : Decoder<System.DateTime> =
        fun path token ->
            if Helpers.isDate token then
                token.Value<System.DateTime>().ToUniversalTime() |> Ok
            elif Helpers.isString token then
                match System.DateTime.TryParse (Helpers.asString token, CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | true, x -> x.ToUniversalTime() |> Ok
                | _ -> (path, BadPrimitive("a datetime", token)) |> Error
            else
                (path, BadPrimitive("a datetime", token)) |> Error

    /// Decode a System.DateTime value using Sytem.DateTime.TryParse, then convert it to UTC.
    let datetimeUtc : Decoder<System.DateTime> =
        fun path token ->
            if Helpers.isDate token then
                token.Value<System.DateTime>().ToUniversalTime() |> Ok
            else if Helpers.isString token then
                match System.DateTime.TryParse (Helpers.asString token) with
                | true, x -> x.ToUniversalTime() |> Ok
                | _ -> (path, BadPrimitive("a datetime", token)) |> Error
            else
                (path, BadPrimitive("a datetime", token)) |> Error

    /// Decode a System.DateTime with DateTime.TryParse; uses default System.DateTimeStyles.
    let datetimeLocal : Decoder<System.DateTime> =
        fun path token ->
            if Helpers.isDate token then
                token.Value<System.DateTime>() |> Ok
            else if Helpers.isString token then
                match System.DateTime.TryParse (Helpers.asString token) with
                | true, x -> x |> Ok
                | _ -> (path, BadPrimitive("a datetime", token)) |> Error
            else
                (path, BadPrimitive("a datetime", token)) |> Error

    let datetimeOffset : Decoder<System.DateTimeOffset> =
        fun path token ->
            if Helpers.isDate token then
                token.Value<System.DateTime>() |> System.DateTimeOffset |> Ok
            elif Helpers.isString token then
                match System.DateTimeOffset.TryParse (Helpers.asString token, CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | true, x -> Ok x
                | _ -> (path, BadPrimitive("a datetimeoffset", token)) |> Error
            else
                (path, BadPrimitive("a datetimeoffset", token)) |> Error

    let timespan : Decoder<System.TimeSpan> =
        fun path token ->
            if token.Type = JTokenType.TimeSpan || token.Type = JTokenType.String then
                match System.TimeSpan.TryParse (Helpers.asString token, CultureInfo.InvariantCulture) with
                | true, x -> Ok x
                | _ -> (path, BadPrimitive("a timespan", token)) |> Error
            else
                (path, BadPrimitive("a timespan", token)) |> Error

    /////////////////////////
    // Object primitives ///
    ///////////////////////

    let private decodeMaybeNull path (decoder : Decoder<'T>) value =
        // The decoder may be an option decoder so give it an opportunity to check null values
        match decoder path value with
        | Ok v -> Ok(Some v)
        | Error _ when Helpers.isNullValue value -> Ok None
        | Error er -> Error er

    let optional (fieldName : string) (decoder : Decoder<'value>) : Decoder<'value option> =
        fun path value ->
            if Helpers.isObject value then
                let fieldValue = Helpers.getField fieldName value
                if Helpers.isUndefined fieldValue then Ok None
                else decodeMaybeNull (path + "." + fieldName) decoder fieldValue
            else
                Error(path, BadType("an object", value))

    let private badPathError fieldNames currentPath value =
        let currentPath = defaultArg currentPath ("$"::fieldNames |> String.concat ".")
        let msg = "an object with path `" + (String.concat "." fieldNames) + "`"
        Error(currentPath, BadPath (msg, value, List.tryLast fieldNames |> Option.defaultValue ""))

    let optionalAt (fieldNames : string list) (decoder : Decoder<'value>) : Decoder<'value option> =
        fun firstPath firstValue ->
            ((firstPath, firstValue, None), fieldNames)
            ||> List.fold (fun (curPath, curValue, res) field ->
                match res with
                | Some _ -> curPath, curValue, res
                | None ->
                    if Helpers.isNullValue curValue then
                        curPath, curValue, Some (Ok None)
                    elif Helpers.isObject curValue then
                        let curValue = Helpers.getField field curValue
                        curPath + "." + field, curValue, None
                    else
                        let res = Error(curPath, BadType("an object", curValue))
                        curPath, curValue, Some res)
            |> function
                | _, _, Some res -> res
                | lastPath, lastValue, None ->
                    if Helpers.isUndefined lastValue then Ok None
                    else decodeMaybeNull lastPath decoder lastValue

    let field (fieldName: string) (decoder : Decoder<'value>) : Decoder<'value> =
        fun path value ->
            if Helpers.isObject value then
                let fieldValue = Helpers.getField fieldName value
                if Helpers.isUndefined fieldValue then
                    Error(path, BadField ("an object with a field named `" + fieldName + "`", value))
                else
                    decoder (path + "." + fieldName) fieldValue
            else
                Error(path, BadType("an object", value))

    let at (fieldNames: string list) (decoder : Decoder<'value>) : Decoder<'value> =
        fun firstPath firstValue ->
            ((firstPath, firstValue, None), fieldNames)
            ||> List.fold (fun (curPath, curValue, res) field ->
                match res with
                | Some _ -> curPath, curValue, res
                | None ->
                    if Helpers.isNullValue curValue then
                        let res = badPathError fieldNames (Some curPath) firstValue
                        curPath, curValue, Some res
                    elif Helpers.isObject curValue then
                        let curValue = Helpers.getField field curValue
                        if Helpers.isUndefined curValue then
                            let res = badPathError fieldNames None firstValue
                            curPath, curValue, Some res
                        else
                            curPath + "." + field, curValue, None
                    else
                        let res = Error(curPath, BadType("an object", curValue))
                        curPath, curValue, Some res)
            |> function
                | _, _, Some res -> res
                | lastPath, lastValue, None ->
                    decoder lastPath lastValue

    let index (requestedIndex: int) (decoder : Decoder<'value>) : Decoder<'value> =
        fun path token ->
            let currentPath = path + ".[" + (Operators.string requestedIndex) + "]"
            if Helpers.isArray token then
                let vArray = Helpers.asArray token
                if requestedIndex < vArray.Length then
                    decoder currentPath (vArray.[requestedIndex])
                else
                    let msg =
                        "a longer array. Need index `"
                            + (requestedIndex.ToString())
                            + "` but there are only `"
                            + (vArray.Length.ToString())
                            + "` entries"

                    (currentPath, TooSmallArray(msg, token))
                    |> Error
            else
                (currentPath, BadPrimitive("an array", token))
                |> Error

    let option (decoder : Decoder<'value>) : Decoder<'value option> =
        fun path value ->
            if Helpers.isNullValue value then Ok None
            else decoder path value |> Result.map Some

    //////////////////////
    // Data structure ///
    ////////////////////

    let list (decoder : Decoder<'value>) : Decoder<'value list> =
        fun path value ->
            if Helpers.isArray value then
                let mutable i = -1
                let tokens = Helpers.asArray value
                (Ok [], tokens) ||> Array.fold (fun acc value ->
                    i <- i + 1
                    match acc with
                    | Error _ -> acc
                    | Ok acc ->
                        match decoder (path + ".[" + (i.ToString()) + "]") value with
                        | Error er -> Error er
                        | Ok value -> Ok (value::acc))
                |> Result.map List.rev
            else
                (path, BadPrimitive ("a list", value))
                |> Error

    let array (decoder : Decoder<'value>) : Decoder<'value array> =
        fun path value ->
            if Helpers.isArray value then
                let mutable i = -1
                let tokens = Helpers.asArray value
                let arr = Array.zeroCreate tokens.Length
                (Ok arr, tokens) ||> Array.fold (fun acc value ->
                    i <- i + 1
                    match acc with
                    | Error _ -> acc
                    | Ok acc ->
                        match decoder (path + ".[" + (i.ToString()) + "]") value with
                        | Error er -> Error er
                        | Ok value -> acc.[i] <- value; Ok acc)
            else
                (path, BadPrimitive ("an array", value))
                |> Error

    let keys : Decoder<string list> =
        fun path value ->
            if Helpers.isObject value then
                let value = value.Value<JObject>()
                value.Properties()
                |> Seq.map (fun prop ->
                    prop.Name
                )
                |> List.ofSeq |> Ok
            else
                (path, BadPrimitive ("an object", value))
                |> Error


    let keyValuePairs (decoder : Decoder<'value>) : Decoder<(string * 'value) list> =
        fun path value ->
            match keys path value with
            | Ok objecKeys ->
                (Ok [], objecKeys ) ||> Seq.fold (fun acc prop ->
                    match acc with
                    | Error _ -> acc
                    | Ok acc ->
                        match Helpers.getField prop value |> decoder path with
                        | Error er -> Error er
                        | Ok value -> (prop, value)::acc |> Ok)
                |> Result.map List.rev
            | Error e -> Error e

    //////////////////////////////
    // Inconsistent Structure ///
    ////////////////////////////

    let oneOf (decoders : Decoder<'value> list) : Decoder<'value> =
        fun path value ->
            let rec runner (decoders : Decoder<'value> list) (errors : string list) =
                match decoders with
                | head::tail ->
                    match fromValue path head value with
                    | Ok v ->
                        Ok v
                    | Error error -> runner tail (errors @ [error])
                | [] -> (path, BadOneOf errors) |> Error

            runner decoders []

    //////////////////////
    // Fancy decoding ///
    ////////////////////

    let nil (output : 'a) : Decoder<'a> =
        fun path token ->
            if Helpers.isNullValue token then
                Ok output
            else
                (path, BadPrimitive("null", token)) |> Error

    let value _ v = Ok v

    let succeed (output : 'a) : Decoder<'a> =
        fun _ _ ->
            Ok output

    let fail (msg: string) : Decoder<'a> =
        fun path _ ->
            (path, FailMessage msg) |> Error

    let andThen (cb: 'a -> Decoder<'b>) (decoder : Decoder<'a>) : Decoder<'b> =
        fun path value ->
            match decoder path value with
            | Error error -> Error error
            | Ok result -> cb result path value

    let all (decoders: Decoder<'a> list): Decoder<'a list> =
        fun path value ->
            let rec runner (decoders: Decoder<'a> list) (values: 'a list) =
                match decoders with
                | decoder :: tail ->
                    match decoder path value with
                    | Ok value -> runner tail (values @ [ value ])
                    | Error error -> Error error
                | [] -> Ok values

            runner decoders []

    /////////////////////
    // Map functions ///
    ///////////////////

    let map
        (ctor : 'a -> 'value)
        (d1 : Decoder<'a>) : Decoder<'value> =
        fun path value ->
            match d1 path value with
            | Ok v1 -> Ok (ctor v1)
            | Error er -> Error er

    let map2
        (ctor : 'a -> 'b -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value with
            | Ok v1, Ok v2 -> Ok (ctor v1 v2)
            | Error er,_ -> Error er
            | _,Error er -> Error er

    let map3
        (ctor : 'a -> 'b -> 'c -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>)
        (d3 : Decoder<'c>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value, d3 path value with
            | Ok v1, Ok v2, Ok v3 -> Ok (ctor v1 v2 v3)
            | Error er,_,_ -> Error er
            | _,Error er,_ -> Error er
            | _,_,Error er -> Error er

    let map4
        (ctor : 'a -> 'b -> 'c -> 'd -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>)
        (d3 : Decoder<'c>)
        (d4 : Decoder<'d>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value, d3 path value, d4 path value with
            | Ok v1, Ok v2, Ok v3, Ok v4 -> Ok (ctor v1 v2 v3 v4)
            | Error er,_,_,_ -> Error er
            | _,Error er,_,_ -> Error er
            | _,_,Error er,_ -> Error er
            | _,_,_,Error er -> Error er

    let map5
        (ctor : 'a -> 'b -> 'c -> 'd -> 'e -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>)
        (d3 : Decoder<'c>)
        (d4 : Decoder<'d>)
        (d5 : Decoder<'e>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value, d3 path value, d4 path value, d5 path value with
            | Ok v1, Ok v2, Ok v3, Ok v4, Ok v5 -> Ok (ctor v1 v2 v3 v4 v5)
            | Error er,_,_,_,_ -> Error er
            | _,Error er,_,_,_ -> Error er
            | _,_,Error er,_,_ -> Error er
            | _,_,_,Error er,_ -> Error er
            | _,_,_,_,Error er -> Error er

    let map6
        (ctor : 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>)
        (d3 : Decoder<'c>)
        (d4 : Decoder<'d>)
        (d5 : Decoder<'e>)
        (d6 : Decoder<'f>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value, d3 path value, d4 path value, d5 path value, d6 path value with
            | Ok v1, Ok v2, Ok v3, Ok v4, Ok v5, Ok v6 -> Ok (ctor v1 v2 v3 v4 v5 v6)
            | Error er,_,_,_,_,_ -> Error er
            | _,Error er,_,_,_,_ -> Error er
            | _,_,Error er,_,_,_ -> Error er
            | _,_,_,Error er,_,_ -> Error er
            | _,_,_,_,Error er,_ -> Error er
            | _,_,_,_,_,Error er -> Error er

    let map7
        (ctor : 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> 'g -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>)
        (d3 : Decoder<'c>)
        (d4 : Decoder<'d>)
        (d5 : Decoder<'e>)
        (d6 : Decoder<'f>)
        (d7 : Decoder<'g>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value, d3 path value, d4 path value, d5 path value, d6 path value, d7 path value with
            | Ok v1, Ok v2, Ok v3, Ok v4, Ok v5, Ok v6, Ok v7 -> Ok (ctor v1 v2 v3 v4 v5 v6 v7)
            | Error er,_,_,_,_,_,_ -> Error er
            | _,Error er,_,_,_,_,_ -> Error er
            | _,_,Error er,_,_,_,_ -> Error er
            | _,_,_,Error er,_,_,_ -> Error er
            | _,_,_,_,Error er,_,_ -> Error er
            | _,_,_,_,_,Error er,_ -> Error er
            | _,_,_,_,_,_,Error er -> Error er

    let map8
        (ctor : 'a -> 'b -> 'c -> 'd -> 'e -> 'f -> 'g -> 'h -> 'value)
        (d1 : Decoder<'a>)
        (d2 : Decoder<'b>)
        (d3 : Decoder<'c>)
        (d4 : Decoder<'d>)
        (d5 : Decoder<'e>)
        (d6 : Decoder<'f>)
        (d7 : Decoder<'g>)
        (d8 : Decoder<'h>) : Decoder<'value> =
        fun path value ->
            match d1 path value, d2 path value, d3 path value, d4 path value, d5 path value, d6 path value, d7 path value, d8 path value with
            | Ok v1, Ok v2, Ok v3, Ok v4, Ok v5, Ok v6, Ok v7, Ok v8 -> Ok (ctor v1 v2 v3 v4 v5 v6 v7 v8)
            | Error er,_,_,_,_,_,_,_ -> Error er
            | _,Error er,_,_,_,_,_,_ -> Error er
            | _,_,Error er,_,_,_,_,_ -> Error er
            | _,_,_,Error er,_,_,_,_ -> Error er
            | _,_,_,_,Error er,_,_,_ -> Error er
            | _,_,_,_,_,Error er,_,_ -> Error er
            | _,_,_,_,_,_,Error er,_ -> Error er
            | _,_,_,_,_,_,_,Error er -> Error er

    //////////////////////
    // Object builder ///
    ////////////////////

    type IRequiredGetter =
        abstract Field : string -> Decoder<'a> -> 'a
        abstract At : List<string> -> Decoder<'a> -> 'a
        abstract Raw : Decoder<'a> -> 'a

    type IOptionalGetter =
        abstract Field : string -> Decoder<'a> -> 'a option
        abstract At : List<string> -> Decoder<'a> -> 'a option
        abstract Raw : Decoder<'a> -> 'a option

    type IGetters =
        abstract Required: IRequiredGetter
        abstract Optional: IOptionalGetter

    let private unwrapWith (errors: ResizeArray<DecoderError>) path (decoder: Decoder<'T>) value: 'T =
        match decoder path value with
        | Ok v -> v
        | Error er -> errors.Add(er); Unchecked.defaultof<'T>

    type Getters<'T>(path: string, v: JsonValue) =
        let mutable errors = ResizeArray<DecoderError>()
        let required =
            { new IRequiredGetter with
                member __.Field (fieldName : string) (decoder : Decoder<_>) =
                    unwrapWith errors path (field fieldName decoder) v
                member __.At (fieldNames : string list) (decoder : Decoder<_>) =
                    unwrapWith errors path (at fieldNames decoder) v
                member __.Raw (decoder: Decoder<_>) =
                    unwrapWith errors path decoder v }
        let optional =
            { new IOptionalGetter with
                member __.Field (fieldName : string) (decoder : Decoder<_>) =
                    unwrapWith errors path (optional fieldName decoder) v
                member __.At (fieldNames : string list) (decoder : Decoder<_>) =
                    unwrapWith errors path (optionalAt fieldNames decoder) v
                member __.Raw (decoder: Decoder<_>) =
                    match decoder path v with
                    | Ok v -> Some v
                    | Error((_, reason) as error) ->
                        match reason with
                        | BadPrimitive(_,v)
                        | BadPrimitiveExtra(_,v,_)
                        | BadType(_,v) ->
                            if Helpers.isNullValue v then None
                            else errors.Add(error); Unchecked.defaultof<_>
                        | BadField _
                        | BadPath _ -> None
                        | TooSmallArray _
                        | FailMessage _
                        | BadOneOf _ -> errors.Add(error); Unchecked.defaultof<_> }
        member __.Errors: _ list = Seq.toList errors
        interface IGetters with
            member __.Required = required
            member __.Optional = optional

    let object (builder: IGetters -> 'value) : Decoder<'value> =
        fun path v ->
            let getters = Getters(path, v)
            let result = builder getters
            match getters.Errors with
            | [] -> Ok result
            | fst::_ as errors ->
                if errors.Length > 1 then
                    let errors = List.map errorToString errors
                    (path, BadOneOf errors) |> Error
                else
                    Error fst

    ///////////////////////
    // Tuples decoders ///
    ////////////////////

    let tuple2 (decoder1: Decoder<'T1>) (decoder2: Decoder<'T2>) : Decoder<'T1 * 'T2> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                succeed (v1, v2)
            )
        )

    let tuple3 (decoder1: Decoder<'T1>)
               (decoder2: Decoder<'T2>)
               (decoder3: Decoder<'T3>) : Decoder<'T1 * 'T2 * 'T3> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                index 2 decoder3
                |> andThen (fun v3 ->
                    succeed (v1, v2, v3)
                )
            )
        )

    let tuple4 (decoder1: Decoder<'T1>)
               (decoder2: Decoder<'T2>)
               (decoder3: Decoder<'T3>)
               (decoder4: Decoder<'T4>) : Decoder<'T1 * 'T2 * 'T3 * 'T4> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                index 2 decoder3
                |> andThen (fun v3 ->
                    index 3 decoder4
                    |> andThen (fun v4 ->
                        succeed (v1, v2, v3, v4)
                    )
                )
            )
        )

    let tuple5 (decoder1: Decoder<'T1>)
               (decoder2: Decoder<'T2>)
               (decoder3: Decoder<'T3>)
               (decoder4: Decoder<'T4>)
               (decoder5: Decoder<'T5>) : Decoder<'T1 * 'T2 * 'T3 * 'T4 * 'T5> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                index 2 decoder3
                |> andThen (fun v3 ->
                    index 3 decoder4
                    |> andThen (fun v4 ->
                        index 4 decoder5
                        |> andThen (fun v5 ->
                            succeed (v1, v2, v3, v4, v5)
                        )
                    )
                )
            )
        )

    let tuple6 (decoder1: Decoder<'T1>)
               (decoder2: Decoder<'T2>)
               (decoder3: Decoder<'T3>)
               (decoder4: Decoder<'T4>)
               (decoder5: Decoder<'T5>)
               (decoder6: Decoder<'T6>) : Decoder<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                index 2 decoder3
                |> andThen (fun v3 ->
                    index 3 decoder4
                    |> andThen (fun v4 ->
                        index 4 decoder5
                        |> andThen (fun v5 ->
                            index 5 decoder6
                            |> andThen (fun v6 ->
                                succeed (v1, v2, v3, v4, v5, v6)
                            )
                        )
                    )
                )
            )
        )

    let tuple7 (decoder1: Decoder<'T1>)
               (decoder2: Decoder<'T2>)
               (decoder3: Decoder<'T3>)
               (decoder4: Decoder<'T4>)
               (decoder5: Decoder<'T5>)
               (decoder6: Decoder<'T6>)
               (decoder7: Decoder<'T7>) : Decoder<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                index 2 decoder3
                |> andThen (fun v3 ->
                    index 3 decoder4
                    |> andThen (fun v4 ->
                        index 4 decoder5
                        |> andThen (fun v5 ->
                            index 5 decoder6
                            |> andThen (fun v6 ->
                                index 6 decoder7
                                |> andThen (fun v7 ->
                                    succeed (v1, v2, v3, v4, v5, v6, v7)
                                )
                            )
                        )
                    )
                )
            )
        )

    let tuple8 (decoder1: Decoder<'T1>)
               (decoder2: Decoder<'T2>)
               (decoder3: Decoder<'T3>)
               (decoder4: Decoder<'T4>)
               (decoder5: Decoder<'T5>)
               (decoder6: Decoder<'T6>)
               (decoder7: Decoder<'T7>)
               (decoder8: Decoder<'T8>) : Decoder<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7 * 'T8> =
        index 0 decoder1
        |> andThen (fun v1 ->
            index 1 decoder2
            |> andThen (fun v2 ->
                index 2 decoder3
                |> andThen (fun v3 ->
                    index 3 decoder4
                    |> andThen (fun v4 ->
                        index 4 decoder5
                        |> andThen (fun v5 ->
                            index 5 decoder6
                            |> andThen (fun v6 ->
                                index 6 decoder7
                                |> andThen (fun v7 ->
                                    index 7 decoder8
                                    |> andThen (fun v8 ->
                                        succeed (v1, v2, v3, v4, v5, v6, v7, v8)
                                    )
                                )
                            )
                        )
                    )
                )
            )
        )

    ///////////
    // Map ///
    /////////

    let dict (decoder : Decoder<'value>) : Decoder<Map<string, 'value>> =
        map Map.ofList (keyValuePairs decoder)

    let map' (keyDecoder : Decoder<'key>) (valueDecoder : Decoder<'value>) : Decoder<Map<'key, 'value>> =
        map Map.ofSeq (array (tuple2 keyDecoder valueDecoder))

    ////////////
    // Enum ///
    /////////

    module Enum =

        let byte<'TEnum when 'TEnum : enum<byte>> : Decoder<'TEnum> =
            byte
            |> andThen (fun value ->
                LanguagePrimitives.EnumOfValue<byte, 'TEnum> value
                |> succeed
            )

        let sbyte<'TEnum when 'TEnum : enum<sbyte>> : Decoder<'TEnum> =
            sbyte
            |> andThen (fun value ->
                LanguagePrimitives.EnumOfValue<sbyte, 'TEnum> value
                |> succeed
            )

        let int16<'TEnum when 'TEnum : enum<int16>> : Decoder<'TEnum> =
            int16
            |> andThen (fun value ->
                LanguagePrimitives.EnumOfValue<int16, 'TEnum> value
                |> succeed
            )

        let uint16<'TEnum when 'TEnum : enum<uint16>> : Decoder<'TEnum> =
            uint16
            |> andThen (fun value ->
                LanguagePrimitives.EnumOfValue<uint16, 'TEnum> value
                |> succeed
            )

        let int<'TEnum when 'TEnum : enum<int>> : Decoder<'TEnum> =
            int
            |> andThen (fun value ->
                LanguagePrimitives.EnumOfValue<int, 'TEnum> value
                |> succeed
            )

        let uint32<'TEnum when 'TEnum : enum<uint32>> : Decoder<'TEnum> =
            uint32
            |> andThen (fun value ->
                LanguagePrimitives.EnumOfValue<uint32, 'TEnum> value
                |> succeed
            )

    //////////////////
    // Reflection ///
    ////////////////

    open FSharp.Reflection

    type private DecoderCrate<'T>(dec: Decoder<'T>) =
        inherit BoxedDecoder()
        override __.Decode(path, token) =
            match dec path token with
            | Ok v -> Ok(box v)
            | Error er -> Error er
        member __.UnboxedDecoder = dec

    let boxDecoder (d: Decoder<'T>): BoxedDecoder =
        DecoderCrate(d) :> BoxedDecoder

    let unboxDecoder<'T> (d: BoxedDecoder): Decoder<'T> =
        (d :?> DecoderCrate<'T>).UnboxedDecoder

    let private autoObject (decoderInfos: (string * BoxedDecoder)[]) (path : string) (value: JsonValue) =
        if not (Helpers.isObject value) then
            (path, BadPrimitive ("an object", value)) |> Error
        else
            (decoderInfos, Ok []) ||> Array.foldBack (fun (name, decoder) acc ->
                match acc with
                | Error _ -> acc
                | Ok result ->
                    Helpers.getField name value
                    |> decoder.BoxedDecoder (path + "." + name)
                    |> Result.map (fun v -> v::result))

    let private mixedArray offset (decoders: BoxedDecoder[]) (path: string) (values: JsonValue[]): Result<obj list, DecoderError> =
        let expectedLength = decoders.Length + offset
        if expectedLength <> values.Length then
            (path, sprintf "Expected array of length %i but got %i" expectedLength values.Length
            |> FailMessage) |> Error
        else
            let mutable result = Ok []
            for i = offset to values.Length - 1 do
                match result with
                | Error _ -> ()
                | Ok acc ->
                    let path = sprintf "%s[%i]" path i
                    let decoder = decoders.[i - offset]
                    let value = values.[i]
                    result <- decoder.Decode(path, value) |> Result.map (fun v -> v::acc)
            result
            |> Result.map List.rev

    let private genericOption t (decoder: BoxedDecoder) =
        let ucis = FSharpType.GetUnionCases(t)
        fun (path : string) (value: JsonValue) ->
            if Helpers.isNullValue value then
                Ok (FSharpValue.MakeUnion(ucis.[0], [||]))
            else
                decoder.Decode(path, value)
                |> Result.map (fun value -> FSharpValue.MakeUnion(ucis.[1], [|value|]))

    let private genericList t (decoder: BoxedDecoder) =
        fun (path : string) (value: JsonValue) ->
            if not (Helpers.isArray value) then
                (path, BadPrimitive ("a list", value)) |> Error
            else
                let values = value.Value<JArray>()
                let ucis = FSharpType.GetUnionCases(t, allowAccessToPrivateRepresentation=true)
                let empty = FSharpValue.MakeUnion(ucis.[0], [||], allowAccessToPrivateRepresentation=true)
                (values, Ok empty) ||> Seq.foldBack (fun value acc ->
                    match acc with
                    | Error _ -> acc
                    | Ok acc ->
                        match decoder.Decode(path, value) with
                        | Error er -> Error er
                        | Ok result -> FSharpValue.MakeUnion(ucis.[1], [|result; acc|], allowAccessToPrivateRepresentation=true) |> Ok)

    // let private genericSeq t (decoder: BoxedDecoder) =
    //     fun (path : string) (value: JsonValue) ->
    //         if not (Helpers.isArray value) then
    //             (path, BadPrimitive ("a seq", value)) |> Error
    //         else
    //             let values = value.Value<JArray>()
    //             let ucis = FSharpType.GetUnionCases(t, allowAccessToPrivateRepresentation=true)
    //             let empty = FSharpValue.MakeUnion(ucis.[0], [||], allowAccessToPrivateRepresentation=true)
    //             (values, Ok empty) ||> Seq.foldBack (fun value acc ->
    //                 match acc with
    //                 | Error _ -> acc
    //                 | Ok acc ->
    //                     match decoder.Decode(path, value) with
    //                     | Error er -> Error er
    //                     | Ok result -> FSharpValue.MakeUnion(ucis.[1], [|result; acc|], allowAccessToPrivateRepresentation=true) |> Ok)

    let private (|StringifiableType|_|) (t: System.Type): (string->obj) option =
        let fullName = t.FullName
        if fullName = typeof<string>.FullName then
            Some box
        elif fullName = typeof<System.Guid>.FullName then
            let ofString = t.GetConstructor([|typeof<string>|])
            Some(fun (v: string) -> ofString.Invoke([|v|]))
        else None

    let inline private enumDecoder<'UnderlineType when 'UnderlineType : equality>
        (decoder : Decoder<'UnderlineType>)
        (toString : 'UnderlineType -> string)
        (t: System.Type) =

            fun path value ->
                match decoder path value with
                | Ok enumValue ->
                    System.Enum.GetValues(t)
                    |> Seq.cast<'UnderlineType>
                    |> Seq.contains enumValue
                    |> function
                    | true ->
                        System.Enum.Parse(t, toString enumValue)
                        |> Ok
                    | false ->
                        (path, BadPrimitiveExtra(t.FullName, value, "Unkown value provided for the enum"))
                        |> Error
                | Error msg ->
                    Error msg

    let rec private genericMap extra isCamelCase (t: System.Type) =

        let keyType   = t.GenericTypeArguments.[0]
        let valueType = t.GenericTypeArguments.[1]
        let valueDecoder = autoDecoder extra isCamelCase false valueType
        let keyDecoder = autoDecoder extra isCamelCase false keyType
        let tupleType = typedefof<obj * obj>.MakeGenericType([|keyType; valueType|])
        let listType = typedefof< ResizeArray<obj> >.MakeGenericType([|tupleType|])
        let addMethod = listType.GetMethod("Add")
        fun (path: string)  (value: JsonValue) ->
            let empty = System.Activator.CreateInstance(listType)
            let kvs =
                if Helpers.isArray value then
                    (Ok empty, value.Value<JArray>()) ||> Seq.fold (fun acc value ->
                        match acc with
                        | Error _ -> acc
                        | Ok acc ->
                            if not (Helpers.isArray value) then
                                (path, BadPrimitive ("an array", value)) |> Error
                            else
                                let kv = value.Value<JArray>()
                                match keyDecoder.Decode(path + "[0]", kv.[0]), valueDecoder.Decode(path + "[1]", kv.[1]) with
                                | Error er, _ -> Error er
                                | _, Error er -> Error er
                                | Ok key, Ok value ->
                                    addMethod.Invoke(acc, [|FSharpValue.MakeTuple([|key; value|], tupleType)|]) |> ignore
                                    Ok acc)
                else
                    match keyType with
                    | StringifiableType ofString when Helpers.isObject value ->
                        (Ok empty, value :?> JObject |> Seq.cast<JProperty>)
                        ||> Seq.fold (fun acc prop ->
                            match acc with
                            | Error _ -> acc
                            | Ok acc ->
                                match valueDecoder.Decode(path + "." + prop.Name, prop.Value) with
                                | Error er -> Error er
                                | Ok v ->
                                    addMethod.Invoke(acc, [|FSharpValue.MakeTuple([|ofString prop.Name; v|], tupleType)|]) |> ignore
                                    Ok acc)
                    | _ ->
                        (path, BadPrimitive ("an array or an object", value)) |> Error
            kvs |> Result.map (fun kvs -> System.Activator.CreateInstance(t, kvs))

    and private makeUnion extra caseStrategy (t : System.Type) (searchedName : string) (path : string) (values: JsonValue[]) =
        let uci =
            FSharpType.GetUnionCases(t, allowAccessToPrivateRepresentation=true)
            |> Array.tryFind (fun uci ->
                #if !NETFRAMEWORK
                match t with
                | Util.Reflection.StringEnum t ->
                    match uci with
                    | Util.Reflection.CompiledName name ->
                        name = searchedName

                    | _ ->
                        match t.ConstructorArguments with
                        | Util.Reflection.LowerFirst ->
                            let adaptedName = uci.Name.[..0].ToLowerInvariant() + uci.Name.[1..]
                            adaptedName = searchedName

                        | Util.Reflection.Forward ->
                            uci.Name = searchedName
                | _ ->
                    uci.Name = searchedName
                #else
                uci.Name = searchedName
                #endif
            )

        match uci with
        | None -> (path, FailMessage("Cannot find case " + searchedName + " in " + t.FullName)) |> Error
        | Some uci ->
            if values.Length <= 1 then // First item is the case name
                FSharpValue.MakeUnion(uci, [||], allowAccessToPrivateRepresentation=true) |> Ok
            else
                let decoders = uci.GetFields() |> Array.map (fun fi -> autoDecoder extra caseStrategy false fi.PropertyType)
                mixedArray 1 decoders path values
                |> Result.map (fun values -> FSharpValue.MakeUnion(uci, List.toArray values, allowAccessToPrivateRepresentation=true))

    and private autoDecodeRecordsAndUnions extra (caseStrategy : CaseStrategy) (isOptional : bool) (t: System.Type): BoxedDecoder =
        // Add the decoder to extra in case one of the fields is recursive
        let decoderRef = ref Unchecked.defaultof<_>
        let extra = extra |> Map.add t.FullName decoderRef
        let decoder =
            if FSharpType.IsRecord(t, allowAccessToPrivateRepresentation=true) then
                let decoders =
                    FSharpType.GetRecordFields(t, allowAccessToPrivateRepresentation=true)
                    |> Array.map (fun fi ->
                        let name = Util.Casing.convert caseStrategy fi.Name
                        name, autoDecoder extra caseStrategy false fi.PropertyType)
                boxDecoder(fun path value ->
                    autoObject decoders path value
                    |> Result.map (fun xs -> FSharpValue.MakeRecord(t, List.toArray xs, allowAccessToPrivateRepresentation=true)))

            elif FSharpType.IsUnion(t, allowAccessToPrivateRepresentation=true) then
                boxDecoder(fun path (value: JsonValue) ->
                    if Helpers.isString(value) then
                        let name = Helpers.asString value
                        makeUnion extra caseStrategy t name path [||]
                    elif Helpers.isArray(value) then
                        let values = Helpers.asArray value
                        string (path + "[0]") values.[0]
                        |> Result.bind (fun name -> makeUnion extra caseStrategy t name path values)
                    else (path, BadPrimitive("a string or array", value)) |> Error)

            else
                if isOptional then
                    // The error will only happen at runtime if the value is not null
                    // See https://github.com/MangelMaxime/Thoth/pull/84#issuecomment-444837773
                    boxDecoder(fun path value -> Error(path, BadType("an extra coder for " + t.FullName, value)))
                else
                    failwithf "Cannot generate auto decoder for %s. Please pass an extra decoder." t.FullName
        decoderRef := decoder
        decoder


    and private autoDecoder (extra: Map<string, ref<BoxedDecoder>>) caseStrategy (isOptional : bool) (t: System.Type) : BoxedDecoder =
      let fullname = t.FullName
      match Map.tryFind fullname extra with
      | Some decoderRef -> boxDecoder(fun path value -> decoderRef.contents.BoxedDecoder path value)
      | None ->
        if t.IsArray then
            let elemType = t.GetElementType()
            let decoder = autoDecoder extra caseStrategy false elemType
            boxDecoder(fun path value ->
                match array decoder.BoxedDecoder path value with
                | Ok items ->
                    let ar = System.Array.CreateInstance(elemType, items.Length)
                    for i = 0 to ar.Length - 1 do
                        ar.SetValue(items.[i], i)
                    Ok ar
                | Error er -> Error er)
        elif t.IsGenericType then
            if FSharpType.IsTuple(t) then
                let decoders = FSharpType.GetTupleElements(t) |> Array.map (autoDecoder extra caseStrategy false)
                boxDecoder(fun path value ->
                    if Helpers.isArray value then
                        mixedArray 0 decoders path (Helpers.asArray value)
                        |> Result.map (fun xs -> FSharpValue.MakeTuple(List.toArray xs, t))
                    else (path, BadPrimitive ("an array", value)) |> Error)
            else
                let fullname = t.GetGenericTypeDefinition().FullName
                if fullname = typedefof<obj option>.FullName then
                    autoDecoder extra caseStrategy true t.GenericTypeArguments.[0] |> genericOption t |> boxDecoder
                elif fullname = typedefof<obj list>.FullName then
                    autoDecoder extra caseStrategy false t.GenericTypeArguments.[0] |> genericList t |> boxDecoder
                // I don't know for now how to support seq
                // elif fullname = typedefof<obj seq>.FullName then
                //     autoDecoder extra caseStrategy false t.GenericTypeArguments.[0] |> genericSeq t |> boxDecoder
                elif fullname = typedefof< Map<System.IComparable, obj> >.FullName then
                    genericMap extra caseStrategy t |> boxDecoder
                elif fullname = typedefof< Set<string> >.FullName then
                    let t = t.GenericTypeArguments.[0]
                    let decoder = autoDecoder extra caseStrategy false t
                    boxDecoder(fun path value ->
                        match array decoder.BoxedDecoder path value with
                        | Ok items ->
                            let ar = System.Array.CreateInstance(t, items.Length)
                            for i = 0 to ar.Length - 1 do
                                ar.SetValue(items.[i], i)
                            let setType = typedefof< Set<string> >.MakeGenericType([|t|])
                            System.Activator.CreateInstance(setType, ar) |> Ok
                        | Error er -> Error er)
                else
                    autoDecodeRecordsAndUnions extra caseStrategy isOptional t
        elif t.IsEnum then
            let enumType = System.Enum.GetUnderlyingType(t).FullName
            if enumType = typeof<sbyte>.FullName then
                enumDecoder<sbyte> sbyte Operators.string t |> boxDecoder
            elif enumType = typeof<byte>.FullName then
                enumDecoder<byte> byte Operators.string t |> boxDecoder
            elif enumType = typeof<int16>.FullName then
                enumDecoder<int16> int16 Operators.string t |> boxDecoder
            elif enumType = typeof<uint16>.FullName then
                enumDecoder<uint16> uint16 Operators.string t |> boxDecoder
            elif enumType = typeof<int>.FullName then
                enumDecoder<int> int Operators.string t |> boxDecoder
            elif enumType = typeof<uint32>.FullName then
                enumDecoder<uint32> uint32 Operators.string t |> boxDecoder
            else
                failwithf
                    """Cannot generate auto decoder for %s.
Thoth.Json.Net only support the folluwing enum types:
- sbyte
- byte
- int16
- uint16
- int
- uint32
If you can't use one of these types, please pass an extra decoder.
                    """ t.FullName
        else
            if fullname = typeof<bool>.FullName then
                boxDecoder bool
            elif fullname = typedefof<unit>.FullName then
                boxDecoder unit
            elif fullname = typeof<string>.FullName then
                boxDecoder string
            elif fullname = typeof<sbyte>.FullName then
                boxDecoder sbyte
            elif fullname = typeof<byte>.FullName then
                boxDecoder byte
            elif fullname = typeof<int16>.FullName then
                boxDecoder int16
            elif fullname = typeof<uint16>.FullName then
                boxDecoder uint16
            elif fullname = typeof<int>.FullName then
                boxDecoder int
            elif fullname = typeof<uint32>.FullName then
                boxDecoder uint32
            elif fullname = typeof<float>.FullName then
                boxDecoder float
            elif fullname = typeof<float32>.FullName then
                boxDecoder float32
            // These number types require extra libraries in Fable. To prevent penalizing
            // all users, extra decoders (withInt64, etc) must be passed when they're needed.

            // elif fullname = typeof<int64>.FullName then
            //     boxDecoder int64
            // elif fullname = typeof<uint64>.FullName then
            //     boxDecoder uint64
            // elif fullname = typeof<bigint>.FullName then
            //     boxDecoder bigint
            // elif fullname = typeof<decimal>.FullName then
            //     boxDecoder decimal
            elif fullname = typeof<System.DateTime>.FullName then
                boxDecoder datetimeUtc
            elif fullname = typeof<System.DateTimeOffset>.FullName then
                boxDecoder datetimeOffset
            elif fullname = typeof<System.TimeSpan>.FullName then
                boxDecoder timespan
            elif fullname = typeof<System.Guid>.FullName then
                boxDecoder guid
            elif fullname = typeof<obj>.FullName then
                boxDecoder (fun _ v ->
                    if Helpers.isNullValue v then Ok(null: obj)
                    else v.Value<obj>() |> Ok)
            else autoDecodeRecordsAndUnions extra caseStrategy isOptional t

    let private makeExtra (extra: ExtraCoders option) =
        match extra with
        | None -> Map.empty
        | Some e -> Map.map (fun _ (_,dec) -> ref dec) e.Coders

    module Auto =

        /// This API  is only implemented inside Thoth.Json.Net for now
        /// The goal of this API is to provide better interop when consuming Thoth.Json.Net from a C# project
        type LowLevel =
            /// ATTENTION: Use this only when other arguments (isCamelCase, extra) don't change
            static member generateDecoderCached<'T> (t:System.Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Decoder<'T> =
                let caseStrategy = defaultArg caseStrategy PascalCase

                let key =
                    t.FullName
                    |> (+) (Operators.string caseStrategy)
                    |> (+) (extra |> Option.map (fun e -> e.Hash) |> Option.defaultValue "")

                let decoderCrate =
                    Cache.Decoders.Value.GetOrAdd(key, fun _ ->
                        autoDecoder (makeExtra extra) caseStrategy false t)

                fun path token ->
                    match decoderCrate.Decode(path, token) with
                    | Ok x -> Ok(x :?> 'T)
                    | Error er -> Error er

            static member generateDecoder<'T> (t: System.Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Decoder<'T> =
                let caseStrategy = defaultArg caseStrategy PascalCase
                let decoderCrate = autoDecoder (makeExtra extra) caseStrategy false t
                fun path token ->
                    match decoderCrate.Decode(path, token) with
                    | Ok x -> Ok(x :?> 'T)
                    | Error er -> Error er

            static member fromString<'T>(json: string, t: System.Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Result<'T, string> =
                let decoder = LowLevel.generateDecoder(t, ?caseStrategy=caseStrategy, ?extra=extra)
                fromString decoder json

    type Auto =
        /// ATTENTION: Use this only when other arguments (isCamelCase, extra) don't change
        static member generateDecoderCached<'T> (?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Decoder<'T> =
            let t = typeof<'T>
            Auto.LowLevel.generateDecoderCached (t, ?caseStrategy = caseStrategy, ?extra = extra)

        static member generateDecoder<'T> (?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Decoder<'T> =
            let t = typeof<'T>
            Auto.LowLevel.generateDecoder(t, ?caseStrategy = caseStrategy, ?extra = extra)

        static member fromString<'T>(json: string, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Result<'T, string> =
            let decoder = Auto.generateDecoder(?caseStrategy=caseStrategy, ?extra=extra)
            fromString decoder json

        static member unsafeFromString<'T>(json: string, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): 'T =
            let decoder = Auto.generateDecoder(?caseStrategy=caseStrategy, ?extra=extra)
            match fromString decoder json with
            | Ok x -> x
            | Error msg -> failwith msg
