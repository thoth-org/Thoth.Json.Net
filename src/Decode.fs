namespace Thoth.Json.Net

open System
open System.Text.Json
open System.Globalization

[<RequireQualifiedAccess>]
module Decode =
    module Helpers =
        let anyToString (token: JsonElement) : string =
            let options = JsonSerializerOptions()
            options.WriteIndented <- true

            JsonSerializer.Serialize(token, options)

        let inline getField (fieldName: string) (token: JsonElement) =
            match token.TryGetProperty fieldName with
            | true, field -> Some field
            | _ -> None
        let inline isBool (token: JsonElement) = token.ValueKind = JsonValueKind.True || token.ValueKind = JsonValueKind.False
        let inline isNumber (token: JsonElement) = token.ValueKind = JsonValueKind.Number
        let inline isString (token: JsonElement) = token.ValueKind = JsonValueKind.String
        let inline isArray (token: JsonElement) = token.ValueKind = JsonValueKind.Array
        let inline arrayToSeq (token: JsonElement) = seq { for elem in token.EnumerateArray() -> elem }
        let inline isObject (token: JsonElement) = token.ValueKind = JsonValueKind.Object
        let inline objToSeq (token: JsonElement) = seq { for prop in token.EnumerateObject() -> prop }
        let inline isUndefined (token: JsonElement) = token.ValueKind = JsonValueKind.Undefined
        let inline isNullValue (token: JsonElement) = token.ValueKind = JsonValueKind.Null
        let inline asBool (token: JsonElement): bool = token.GetBoolean()
        let inline asInt (token: JsonElement): int = token.GetInt32()
        let inline asInt64 (token: JsonElement): int64 = token.GetInt64()
        let inline asFloat (token: JsonElement): float = token.GetDouble()
        let inline asFloat32 (token: JsonElement): float32 = token.GetSingle()
        let inline asDecimal (token: JsonElement): Decimal = token.GetDecimal()
        let inline asString (token: JsonElement): string = token.GetString()

    let private genericMsg msg value newLine =
        try
            $"""Expecting {msg} but instead got:{if newLine then "\n" else " "}{Helpers.anyToString value}"""
        with
            | _ ->
                $"""Expecting {msg} but decoder failed. Couldn't report given value due to circular structure.{if newLine then "\n" else " "}"""

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
                genericMsg msg value true + ("\nNode `" + fieldName + "` is unknown.")
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

    /// <summary>
    /// Runs the decoder against the given JSON value.
    ///
    /// If the decoder fails, it reports the error prefixed with the given path.
    ///
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// module Decode =
    ///     let fromRootValue (decoder : Decoder&lt;'T&gt;) =
    ///         Decode.fromValue "$" decoder
    /// </code>
    /// </example>
    /// <param name="path">Path used to report the error</param>
    /// <param name="decoder">Decoder to apply</param>
    /// <param name="value">JSON value to decoder</param>
    /// <returns>
    /// Returns <c>Ok</c> if the decoder succeeds, otherwise <c>Error</c> with the error message.
    /// </returns>
    let fromValue (path : string) (decoder : Decoder<'T>) =
        fun value ->
            match decoder path value with
            | Ok success ->
                Ok success
            | Error error ->
                Error (errorToString error)

    /// <summary>
    /// Parse the provided string in as JSON and then run the decoder against it.
    /// </summary>
    /// <param name="decoder">Decoder to apply</param>
    /// <param name="value">JSON string to decode</param>
    /// <returns>
    /// Returns <c>Ok</c> if the decoder succeeds, otherwise <c>Error</c> with the error message.
    /// </returns>
    let fromString (decoder : Decoder<'T>) =
        fun (value: string) ->
            try
                let rootJsonElement = JsonDocument.Parse(value).RootElement
                fromValue "$" decoder rootJsonElement
            with
                | :? JsonException as ex ->
                    Error("Given an invalid JSON: " + ex.Message)

    /// <summary>
    /// Parse the provided string in as JSON and then run the decoder against it.
    /// </summary>
    /// <param name="decoder">Decoder to apply</param>
    /// <param name="value">JSON string to decode</param>
    /// <returns>
    /// Return the decoded value if the decoder succeeds, otherwise throws an exception.
    /// </returns>
    let unsafeFromString (decoder : Decoder<'T>) =
        fun value ->
            match fromString decoder value with
            | Ok x -> x
            | Error msg -> failwith msg

    //////////////////
    // Primitives ///
    ////////////////

    let string : Decoder<string> =
        fun path token ->
            if Helpers.isString token then
                Ok <| Helpers.asString token
            else
                Error (path, BadPrimitive("a string", token))

    let char : Decoder<char> =
        fun path value ->
            if Helpers.isString value then
                let str = Helpers.asString value
                if str.Length = 1 then
                    Ok str[0]
                else
                    Error (path, BadPrimitive("a single-character string", value))
            else
                Error (path, BadPrimitive("a char", value))

    let guid : Decoder<Guid> =
        fun path value ->
            if Helpers.isString value then
                match value.TryGetGuid() with
                | true, guid -> Ok guid
                | _ -> Error (path, BadPrimitive("a guid", value))
            else
                Error (path, BadPrimitive("a guid", value))

    let unit : Decoder<unit> =
        fun path value ->
            if Helpers.isNullValue value || Helpers.isUndefined value then
                Ok ()
            else
                Error (path, BadPrimitive("null", value))

    let inline private integral
        (name: string)
        (rangeDescription: string)
        (tryGet: JsonElement -> 'T option)
        (tryParse : string -> bool * 'T) : Decoder<'T>
        =
        fun path value ->
            if Helpers.isNumber value then
                match tryGet value with
                | Some got -> Ok got
                | None -> Error (path, BadPrimitiveExtra(name, value, $"not an integral value or not within the allowed range of {rangeDescription}"))
            elif Helpers.isString value then
                match tryParse (Helpers.asString value) with
                | true, parsed -> Ok parsed
                | _ -> Error (path, BadPrimitiveExtra(name, value, $"not an integral value or not within the allowed range of {rangeDescription}"))
            else
                Error (path, BadPrimitive(name, value))

    let sbyte : Decoder<sbyte> =
        integral
            "an sbyte"
            "-128 to 127"
            (fun jEl ->
                jEl.TryGetSByte()
                |> function
                    | true, sbyte -> Some sbyte
                    | _ -> None)
            SByte.TryParse

    /// Alias to Decode.uint8
    let byte : Decoder<byte> =
        integral
            "a byte"
            "0 to 255"
            (fun jEl ->
                jEl.TryGetByte()
                |> function
                    | true, byte -> Some byte
                    | _ -> None)
            Byte.TryParse

    let int16 : Decoder<int16> =
        integral
            "an int16"
            "-32768 to 32767"
            (fun jEl ->
                jEl.TryGetInt16()
                |> function
                    | true, int16 -> Some int16
                    | _ -> None)
            Int16.TryParse

    let uint16 : Decoder<uint16> =
        integral
            "a uint16"
            "0 to 65535"
            (fun jEl ->
                jEl.TryGetUInt16()
                |> function
                    | true, uint16 -> Some uint16
                    | _ -> None)
            UInt16.TryParse

    let int : Decoder<int> =
        integral
            "an int"
            "-2147483648 to 2147483647"
            (fun jEl ->
                jEl.TryGetInt32()
                |> function
                    | true, int32 -> Some int32
                    | _ -> None)
            Int32.TryParse

    let uint32 : Decoder<uint32> =
        integral
            "a uint32"
            "0 to 4294967295"
            (fun jEl ->
                jEl.TryGetUInt32()
                |> function
                    | true, uint32 -> Some uint32
                    | _ -> None)
            UInt32.TryParse

    let int64 : Decoder<int64> =
        integral
            "an int64"
            "-9223372036854775808 to 9223372036854775807"
            (fun jEl ->
                jEl.TryGetInt64()
                |> function
                    | true, int64 -> Some int64
                    | _ -> None)
            Int64.TryParse

    let uint64 : Decoder<uint64> =
        integral
            "a uint64"
            "0 to 18446744073709551615"
            (fun jEl ->
                jEl.TryGetUInt64()
                |> function
                    | true, uint64 -> Some uint64
                    | _ -> None)
            UInt64.TryParse

    let bigint : Decoder<bigint> =
        fun path token ->
            if Helpers.isNumber token then
                Ok <| (Helpers.asInt token |> bigint)
            elif Helpers.isString token then
                match bigint.TryParse (Helpers.asString token, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, bigint -> Ok bigint
                | _ -> Error (path, BadPrimitive("a bigint", token))
            else
                Error (path, BadPrimitive("a bigint", token))

    let bool : Decoder<bool> =
        fun path token ->
            if Helpers.isBool token then
                Ok <| Helpers.asBool token
            else
                Error (path, BadPrimitive("a boolean", token))

    let float : Decoder<float> =
        fun path token ->
            if Helpers.isNumber token then
                Ok <| Helpers.asFloat token
            else
                (path, BadPrimitive("a float", token)) |> Error

    let float32 : Decoder<float32> =
        fun path token ->
            if Helpers.isNumber token then
                Ok <| Helpers.asFloat32 token
            else
                Error (path, BadPrimitive("a float", token))

    let decimal : Decoder<decimal> =
        fun path token ->
            if Helpers.isNumber token then
                Ok <| Helpers.asDecimal token
            elif Helpers.isString token then
                match Decimal.TryParse (Helpers.asString token, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, parsed -> Ok parsed
                | _ -> Error (path, BadPrimitive("a decimal", token))
            else
                Error (path, BadPrimitive("a decimal", token))

    let datetimeUtc : Decoder<DateTime> =
        fun path token ->
            if Helpers.isString token then
                match token.TryGetDateTime() with
                | true, dateTime -> Ok <| dateTime.ToUniversalTime()
                | _ -> Error (path, BadPrimitive("a datetime", token))
            else
                Error (path, BadPrimitive("a datetime", token))

    /// Decode a System.DateTime with DateTime.TryParse; uses default System.DateTimeStyles.
    let datetimeLocal : Decoder<DateTime> =
        fun path token ->
            if Helpers.isString token then
                match token.TryGetDateTime() with
                | true, dateTime -> Ok dateTime
                | _ -> Error (path, BadPrimitive("a datetime", token))
            else
                Error (path, BadPrimitive("a datetime", token))

    let datetimeOffset : Decoder<DateTimeOffset> =
        fun path token ->
            if Helpers.isString token then
                match token.TryGetDateTimeOffset() with
                | true, dateTimeOffset -> Ok dateTimeOffset
                | _ -> Error (path, BadPrimitive("a datetimeoffset", token))
            else
                Error (path, BadPrimitive("a datetimeoffset", token))

    let timespan : Decoder<TimeSpan> =
        fun path token ->
            if Helpers.isString token then
                match TimeSpan.TryParse (Helpers.asString token, CultureInfo.InvariantCulture) with
                | true, timeSpan -> Ok timeSpan
                | _ -> Error (path, BadPrimitive("a timespan", token))
            else
                Error(path, BadPrimitive("a timespan", token))

    /////////////////////////
    // Object primitives ///
    ///////////////////////

    let private decodeMaybeNull path (decoder : Decoder<'T>) value =
        // The decoder may be an option decoder so give it an opportunity to check null values

        // We catch the null value case first to avoid executing the decoder logic
        // Indeed, if the decoder logic try to access the value to do something with it,
        // it can throw an exception about the value being null
        if Helpers.isNullValue value then
            Ok None
        else
            match decoder path value with
            | Ok v -> Ok <| Some v
            | Error err -> Error err

    let optional (fieldName: string) (decoder: Decoder<'value>) : Decoder<'value option> =
        fun path value ->
            if Helpers.isObject value then
                match Helpers.getField fieldName value with
                | Some field ->
                    if Helpers.isUndefined field then
                        Ok None
                    else
                        decodeMaybeNull $"{path}.{fieldName}" decoder field
                | None -> Ok None
            else
                Error(path, BadType("an object", value))

    let private badPathError fieldNames currentPath value =
        let currentPath = defaultArg currentPath ("$"::fieldNames |> String.concat ".")
        let msg = $"""an object with path `{String.concat "." fieldNames}`"""
        Error (currentPath, BadPath (msg, value, List.tryLast fieldNames |> Option.defaultValue ""))

    let optionalAt (fieldNames: string list) (decoder: Decoder<'value>) : Decoder<'value option> =
        fun firstPath firstValue ->
            ((firstPath, firstValue, None), fieldNames)
            ||> List.fold (fun (curPath, curValue, result) field ->
                match result with
                | Some _ -> curPath, curValue, result
                | None ->
                    if Helpers.isNullValue curValue then
                        curPath, curValue, Some <| Ok None
                    elif Helpers.isObject curValue then
                        let maybeNextValue = Helpers.getField field curValue

                        match maybeNextValue with
                        | Some nextValue ->
                            $"{curPath}.{field}", nextValue, None
                        | None ->
                            curPath, curValue, Some <| Ok None
                    else
                        let res = Error (curPath, BadType("an object", curValue))
                        curPath, curValue, Some res)
            |> function
                | _, _, Some result -> result
                | lastPath, lastValue, None ->
                    if Helpers.isUndefined lastValue then
                        Ok None
                    else
                        decodeMaybeNull lastPath decoder lastValue

    let field (fieldName: string) (decoder : Decoder<'value>) : Decoder<'value> =
        fun path value ->
            if Helpers.isObject value then
                match Helpers.getField fieldName value with
                | Some field ->
                    if Helpers.isUndefined field then
                        Error (path, BadField ($"an object with a field named `{fieldName}`", value))
                    else
                        decoder $"{path}.{fieldName}" field
                | None ->
                    Error (path, BadField ($"an object with a field named `{fieldName}`", value))
            else
                Error (path, BadType("an object", value))

    let at (fieldNames: string list) (decoder : Decoder<'value>) : Decoder<'value> =
        fun firstPath firstValue ->
            ((firstPath, firstValue, None), fieldNames)
            ||> List.fold (fun (curPath, curValue, res) field ->
                match res with
                | Some _ -> curPath, curValue, res
                | None ->
                    if Helpers.isNullValue curValue then
                        let result = badPathError fieldNames (Some curPath) firstValue
                        curPath, curValue, Some result
                    elif Helpers.isObject curValue then
                        let maybeNextValue = Helpers.getField field curValue

                        match maybeNextValue with
                        | Some nextValue ->
                            if Helpers.isUndefined nextValue then
                                let result = badPathError fieldNames None firstValue
                                curPath, nextValue, Some result
                            else
                                $"{curPath}.{field}", nextValue, None
                        | None ->
                            let result = badPathError fieldNames None firstValue
                            curPath, curValue, Some result
                    else
                        let res = Error (curPath, BadType("an object", curValue))
                        curPath, curValue, Some res)
            |> function
                | _, _, Some res -> res
                | lastPath, lastValue, None ->
                    decoder lastPath lastValue

    let index (requestedIndex: int) (decoder : Decoder<'value>) : Decoder<'value> =
        fun path token ->
            let currentPath = $"{path}[{Operators.string requestedIndex}]"

            if Helpers.isArray token then
                let arrLength = token.GetArrayLength()

                if requestedIndex < arrLength then
                    decoder currentPath token[requestedIndex]
                else
                    let msg =
                        $"a longer array. Need index `{requestedIndex}` but there are only `{arrLength}` entries"

                    Error (currentPath, TooSmallArray(msg, token))
            else
                Error (currentPath, BadPrimitive("an array", token))

    let option (decoder : Decoder<'value>) : Decoder<'value option> =
        fun path value ->
            if Helpers.isNullValue value then
                Ok None
            else
                decoder path value
                |> Result.map Some

    //////////////////////
    // Data structure ///
    ////////////////////

    let list (decoder : Decoder<'value>) : Decoder<'value list> =
        fun path value ->
            if Helpers.isArray value then
                let mutable i = -1
                let mutable acc = Ok []

                for token in value.EnumerateArray() do
                    i <- i + 1
                    acc <-
                        match acc with
                        | Error _ -> acc
                        | Ok acc ->
                            match decoder $"{path}[{i}]" token with
                            | Error err -> Error err
                            | Ok decoded -> Ok (decoded::acc)

                acc |> Result.map List.rev
            else
                Error (path, BadPrimitive ("a list", value))

    let array (decoder : Decoder<'value>) : Decoder<'value array> =
        fun path value ->
            if Helpers.isArray value then
                let mutable i = -1
                let mutable acc = Ok(Array.zeroCreate <| value.GetArrayLength())

                for token in value.EnumerateArray() do
                    i <- i + 1
                    acc <-
                        match acc with
                        | Error _ -> acc
                        | Ok arr ->
                            match decoder $"{path}[{i}]" token with
                            | Error err -> Error err
                            | Ok decoded ->
                                arr[i] <- decoded
                                Ok arr

                acc
            else
                Error (path, BadPrimitive ("an array", value))

    let keys : Decoder<string list> =
        fun path value ->
            if Helpers.isObject value then
                let mutable acc = []

                for jsonProperty in value.EnumerateObject() do
                    acc <- jsonProperty.Name::acc

                Ok (acc |> List.rev)
            else
                Error (path, BadPrimitive ("an object", value))


    let keyValuePairs (decoder : Decoder<'value>) : Decoder<(string * 'value) list> =
        fun path value ->
            match keys path value with
            | Ok keys ->
                (Ok [], keys) ||> Seq.fold (fun acc key ->
                    match acc with
                    | Error _ -> acc
                    | Ok acc ->
                        match Helpers.getField key value |> Option.get |> decoder path with
                        | Error er -> Error er
                        | Ok value -> (key, value)::acc |> Ok)
                |> Result.map List.rev
            | Error err -> Error err

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

    /// <summary>
    /// Allow to incrementally apply a decoder, for building large objects.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// type Point =
    ///     {
    ///         X : float
    ///         Y : float
    ///     }
    ///
    /// module Point =
    ///     let create x y = { X = x; Y = y }
    ///
    ///     let decode =
    ///         Decode.succeed create
    ///             |> Decode.andMap (Decode.field "x" Decode.float)
    ///             |> Decode.andMap (Decode.field "y" Decode.float)
    /// </code>
    /// </example>
    let andMap<'a, 'b> : 'a Decoder -> ('a -> 'b) Decoder -> 'b Decoder =
        map2 (|>)

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

    type Getters<'T>(path: string, v: JsonElement) =
        let mutable errors = ResizeArray<DecoderError>()

        let required =
            { new IRequiredGetter with
                member _.Field (fieldName : string) (decoder : Decoder<_>) =
                    unwrapWith errors path (field fieldName decoder) v
                member _.At (fieldNames : string list) (decoder : Decoder<_>) =
                    unwrapWith errors path (at fieldNames decoder) v
                member _.Raw (decoder: Decoder<_>) =
                    unwrapWith errors path decoder v }

        let optional =
            { new IOptionalGetter with
                member _.Field (fieldName : string) (decoder : Decoder<_>) =
                    unwrapWith errors path (optional fieldName decoder) v
                member _.At (fieldNames : string list) (decoder : Decoder<_>) =
                    unwrapWith errors path (optionalAt fieldNames decoder) v
                member _.Raw (decoder: Decoder<_>) =
                    match decoder path v with
                    | Ok v -> Some v
                    | Error(_, reason as error) ->
                        match reason with
                        | BadPrimitive(_, v)
                        | BadPrimitiveExtra(_, v, _)
                        | BadType(_, v) ->
                            if Helpers.isNullValue v then None
                            else errors.Add(error); Unchecked.defaultof<_>
                        | BadField _
                        | BadPath _ -> None
                        | TooSmallArray _
                        | FailMessage _
                        | BadOneOf _ ->
                            errors.Add(error)
                            Unchecked.defaultof<_> }

        member _.Errors: _ list = Seq.toList errors

        interface IGetters with
            member _.Required = required
            member _.Optional = optional

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
        override _.Decode(path, token) =
            match dec path token with
            | Ok v -> Ok(box v)
            | Error er -> Error er
        member _.UnboxedDecoder = dec

    let boxDecoder (d: Decoder<'T>): BoxedDecoder =
        DecoderCrate(d) :> BoxedDecoder

    let unboxDecoder<'T> (d: BoxedDecoder): Decoder<'T> =
        (d :?> DecoderCrate<'T>).UnboxedDecoder

    let private autoObject (decoderInfos: (string * BoxedDecoder)[]) (path : string) (value: JsonElement) =
        if not (Helpers.isObject value) then
            Error (path, BadPrimitive ("an object", value))
        else
            (decoderInfos, Ok []) ||> Array.foldBack (fun (name, decoder) acc ->
                match acc with
                | Error _ -> acc
                | Ok result ->
                    Helpers.getField name value
                    |> Option.defaultValue Unchecked.defaultof<JsonElement>
                    |> decoder.BoxedDecoder $"{path}.{name}"
                    |> Result.map (fun v -> v::result))

    let private mixedArray offset (decoders: BoxedDecoder[]) (path: string) (values: JsonElement[]): Result<obj list, DecoderError> =
        let expectedLength = decoders.Length + offset
        if expectedLength <> values.Length then
            (path, $"Expected array of length %i{expectedLength} but got %i{values.Length}"
            |> FailMessage) |> Error
        else
            let mutable result = Ok []
            for i = offset to values.Length - 1 do
                match result with
                | Error _ -> ()
                | Ok acc ->
                    let path = $"%s{path}[%i{i}]"
                    let decoder = decoders[i - offset]
                    let value = values[i]
                    result <- decoder.Decode(path, value) |> Result.map (fun v -> v::acc)
            result
            |> Result.map List.rev

    let private genericOption t (decoder: BoxedDecoder) =
        let ucis = FSharpType.GetUnionCases(t)
        fun (path : string) (value: JsonElement) ->
            if Helpers.isNullValue value || Helpers.isUndefined value then
                Ok (FSharpValue.MakeUnion(ucis[0], [||]))
            else
                decoder.Decode(path, value)
                |> Result.map (fun value -> FSharpValue.MakeUnion(ucis[1], [|value|]))

    let private genericList t (decoder: BoxedDecoder) =
        fun (path : string) (value: JsonElement) ->
            if not <| Helpers.isArray value then
                (path, BadPrimitive ("a list", value)) |> Error
            else
                let ucis = FSharpType.GetUnionCases(t, allowAccessToPrivateRepresentation=true)
                let empty = FSharpValue.MakeUnion(ucis[0], [||], allowAccessToPrivateRepresentation=true)

                (Helpers.arrayToSeq value, Ok empty) ||> Seq.foldBack (fun value acc ->
                    match acc with
                    | Error _ -> acc
                    | Ok acc ->
                        match decoder.Decode(path, value) with
                        | Error er -> Error er
                        | Ok result -> FSharpValue.MakeUnion(ucis[1], [|result; acc|], allowAccessToPrivateRepresentation=true) |> Ok)

    let private (|StringifiableType|_|) (t: Type): (string->obj) option =
        let fullName = t.FullName
        if fullName = typeof<string>.FullName then
            Some box
        elif fullName = typeof<Guid>.FullName then
            let ofString = t.GetConstructor([|typeof<string>|])
            Some(fun (v: string) -> ofString.Invoke([|v|]))
        else None

    let inline private enumDecoder<'UnderlineType when 'UnderlineType : equality>
        (decoder : Decoder<'UnderlineType>)
        (toString : 'UnderlineType -> string)
        (t: Type) =
            fun path value ->
                match decoder path value with
                | Ok enumValue ->
                    Enum.GetValues(t)
                    |> Seq.cast<'UnderlineType>
                    |> Seq.contains enumValue
                    |> function
                    | true ->
                        Enum.Parse(t, toString enumValue)
                        |> Ok
                    | false ->
                        (path, BadPrimitiveExtra(t.FullName, value, "Unknown value provided for the enum"))
                        |> Error
                | Error msg ->
                    Error msg

    let rec private genericMap extra isCamelCase (t: Type) =
        let keyType   = t.GenericTypeArguments[0]
        let valueType = t.GenericTypeArguments[1]
        let valueDecoder = autoDecoder extra isCamelCase false valueType
        let keyDecoder = autoDecoder extra isCamelCase false keyType
        let tupleType = typedefof<obj * obj>.MakeGenericType([|keyType; valueType|])
        let listType = typedefof< ResizeArray<obj> >.MakeGenericType([|tupleType|])
        let addMethod = listType.GetMethod("Add")
        fun (path: string)  (value: JsonElement) ->
            let empty = Activator.CreateInstance(listType)
            let kvs =
                if Helpers.isArray value then
                    (Ok empty, Helpers.arrayToSeq value) ||> Seq.fold (fun acc value ->
                        match acc with
                        | Error _ -> acc
                        | Ok acc ->
                            if not (Helpers.isArray value) then
                                (path, BadPrimitive ("an array", value)) |> Error
                            else
                                let kv = Helpers.arrayToSeq value
                                match keyDecoder.Decode(path + "[0]", kv |> Seq.item 0), valueDecoder.Decode(path + "[1]", kv |> Seq.item 1) with
                                | Error er, _ -> Error er
                                | _, Error er -> Error er
                                | Ok key, Ok value ->
                                    addMethod.Invoke(acc, [|FSharpValue.MakeTuple([|key; value|], tupleType)|]) |> ignore
                                    Ok acc)
                else
                    match keyType with
                    | StringifiableType ofString when Helpers.isObject value ->
                        (Ok empty, Helpers.objToSeq value)
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
            kvs |> Result.map (fun kvs -> Activator.CreateInstance(t, kvs))

    and private makeUnion extra caseStrategy (t : Type) (searchedName : string) (path : string) (values: JsonElement[]) =
        let uci =
            FSharpType.GetUnionCases(t, allowAccessToPrivateRepresentation=true)
            |> Array.tryFind (fun uci -> uci.Name = searchedName)

        match uci with
        | None -> (path, FailMessage("Cannot find case " + searchedName + " in " + t.FullName)) |> Error
        | Some uci ->
            let decoders = uci.GetFields() |> Array.map (fun fi -> autoDecoder extra caseStrategy false fi.PropertyType)
            let values =
                if decoders.Length = 0 && values.Length <= 1 // First item is the case name
                then Ok []
                else mixedArray 1 decoders path values
            values |> Result.map (fun values -> FSharpValue.MakeUnion(uci, List.toArray values, allowAccessToPrivateRepresentation=true))

    and private autoDecodeRecordsAndUnions extra (caseStrategy : CaseStrategy) (isOptional : bool) (t: Type): BoxedDecoder =
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
                boxDecoder(fun path (value: JsonElement) ->
                    if Helpers.isString(value) then
                        let name = Helpers.asString value
                        makeUnion extra caseStrategy t name path [||]
                    elif Helpers.isArray(value) then
                        let values = Helpers.arrayToSeq value |> Array.ofSeq
                        string $"{path}[0]" values[0]
                        |> Result.bind (fun name -> makeUnion extra caseStrategy t name path values)
                    else (path, BadPrimitive("a string or array", value)) |> Error)

            else
                if isOptional then
                    // The error will only happen at runtime if the value is not null
                    // See https://github.com/MangelMaxime/Thoth/pull/84#issuecomment-444837773
                    boxDecoder(fun path value -> Error(path, BadType("an extra coder for " + t.FullName, value)))
                else
                    failwith $"""Cannot generate auto decoder for %s{t.FullName}. Please pass an extra decoder.

Documentation available at: https://thoth-org.github.io/Thoth.Json/documentation/auto/extra-coders.html#ready-to-use-extra-coders"""

        decoderRef.Value <- decoder
        decoder


    and private autoDecoder (extra: Map<string, ref<BoxedDecoder>>) caseStrategy (isOptional : bool) (t: Type) : BoxedDecoder =
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
                    let ar = Array.CreateInstance(elemType, items.Length)
                    for i = 0 to ar.Length - 1 do
                        ar.SetValue(items[i], i)
                    Ok ar
                | Error er -> Error er)
        elif t.IsGenericType then
            if FSharpType.IsTuple(t) then
                let decoders = FSharpType.GetTupleElements(t) |> Array.map (autoDecoder extra caseStrategy false)
                boxDecoder(fun path value ->
                    if Helpers.isArray value then
                        mixedArray 0 decoders path (Helpers.arrayToSeq value |> Array.ofSeq)
                        |> Result.map (fun xs -> FSharpValue.MakeTuple(List.toArray xs, t))
                    else (path, BadPrimitive ("an array", value)) |> Error)
            else
                let fullname = t.GetGenericTypeDefinition().FullName
                if fullname = typedefof<obj option>.FullName then
                    autoDecoder extra caseStrategy true t.GenericTypeArguments[0] |> genericOption t |> boxDecoder
                elif fullname = typedefof<obj list>.FullName then
                    autoDecoder extra caseStrategy false t.GenericTypeArguments[0] |> genericList t |> boxDecoder
                // I don't know for now how to support seq
                // elif fullname = typedefof<obj seq>.FullName then
                //     autoDecoder extra caseStrategy false t.GenericTypeArguments.[0] |> genericSeq t |> boxDecoder
                elif fullname = typedefof< Map<IComparable, obj> >.FullName then
                    genericMap extra caseStrategy t |> boxDecoder
                elif fullname = typedefof< Set<string> >.FullName then
                    let t = t.GenericTypeArguments[0]
                    let decoder = autoDecoder extra caseStrategy false t
                    boxDecoder(fun path value ->
                        match array decoder.BoxedDecoder path value with
                        | Ok items ->
                            let ar = Array.CreateInstance(t, items.Length)
                            for i = 0 to ar.Length - 1 do
                                ar.SetValue(items[i], i)
                            let setType = typedefof< Set<string> >.MakeGenericType([|t|])
                            Activator.CreateInstance(setType, ar) |> Ok
                        | Error er -> Error er)
                else
                    autoDecodeRecordsAndUnions extra caseStrategy isOptional t
        elif t.IsEnum then
            let enumType = Enum.GetUnderlyingType(t).FullName
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

Documentation available at: https://thoth-org.github.io/Thoth.Json/documentation/auto/extra-coders.html#ready-to-use-extra-coders
                    """ t.FullName
        else
            if fullname = typeof<bool>.FullName then
                boxDecoder bool
            elif fullname = typedefof<unit>.FullName then
                boxDecoder unit
            elif fullname = typeof<string>.FullName then
                boxDecoder string
            elif fullname = typeof<char>.FullName then
                boxDecoder char
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
            elif fullname = typeof<DateTime>.FullName then
                boxDecoder datetimeUtc
            elif fullname = typeof<DateTimeOffset>.FullName then
                boxDecoder datetimeOffset
            elif fullname = typeof<TimeSpan>.FullName then
                boxDecoder timespan
            elif fullname = typeof<Guid>.FullName then
                boxDecoder guid
            elif fullname = typeof<obj>.FullName then
                boxDecoder (fun _ v ->
                    if Helpers.isNullValue v then
                        Ok (null: obj)
                    else
                        Ok <| v.Deserialize<obj>())
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
            static member generateDecoderCached<'T> (t:Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Decoder<'T> =
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

            static member generateDecoder<'T> (t: Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Decoder<'T> =
                let caseStrategy = defaultArg caseStrategy PascalCase
                let decoderCrate = autoDecoder (makeExtra extra) caseStrategy false t
                fun path token ->
                    match decoderCrate.Decode(path, token) with
                    | Ok x -> Ok(x :?> 'T)
                    | Error er -> Error er

            static member fromString<'T>(json: string, t: Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders): Result<'T, string> =
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
