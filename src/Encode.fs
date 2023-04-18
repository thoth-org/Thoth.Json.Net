namespace Thoth.Json.Net

open System.Text
open System.Text.Json.Nodes

[<RequireQualifiedAccess>]
module Encode =
    open System.Globalization
    open System.Collections.Generic
    open System.IO
    open System.Text.Json

    ///**Description**
    /// Encode a string
    ///
    ///**Parameters**
    ///  * `value` - parameter of type `string`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let string (value : string) : JsonNode =
        JsonValue.Create(value)

    let inline char (value : char) : JsonNode =
        JsonValue.Create(value)

    ///**Description**
    /// Encode a GUID
    ///
    ///**Parameters**
    ///  * `value` - parameter of type `System.Guid`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let guid (value : System.Guid) : JsonNode =
        value.ToString() |> string

    ///**Description**
    /// Encode a Float. `Infinity` and `NaN` are encoded as `null`.
    ///
    ///**Parameters**
    ///  * `value` - parameter of type `float`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let float (value : float) : JsonNode =
        JsonValue.Create(value)

    let float32 (value : float32) : JsonNode =
        JsonValue.Create(value)

    ///**Description**
    /// Encode a Decimal.
    ///
    ///**Parameters**
    ///  * `value` - parameter of type `decimal`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let decimal (value: decimal) : JsonNode =
        JsonValue.Create(value.ToString(CultureInfo.InvariantCulture))

    ///**Description**
    /// Encode null
    ///
    ///**Parameters**
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let nil: JsonNode =
        JsonValue.Create(null)

    ///**Description**
    /// Encode a bool
    ///**Parameters**
    ///  * `value` - parameter of type `bool`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let bool (value: bool) : JsonNode =
        JsonValue.Create(value)

    ///**Description**
    /// Encode an object
    ///
    ///**Parameters**
    ///  * `values` - parameter of type `(string * Value) list`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let object (values: (string * JsonNode) list) : JsonNode =
        let kvps =
            values
            |> Seq.map (fun (key, value) -> KeyValuePair(key, value))

        JsonObject(kvps)

    ///**Description**
    /// Encode an array
    ///
    ///**Parameters**
    ///  * `values` - parameter of type `Value array`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let array (values: JsonNode array) : JsonNode =
        JsonArray(values)

    ///**Description**
    /// Encode a list
    ///**Parameters**
    ///  * `values` - parameter of type `Value list`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let list (values: JsonNode list) : JsonNode =
        JsonArray(Array.ofList values)

    let seq (values: JsonNode seq) : JsonNode =
        JsonArray(Array.ofSeq values)

    ///**Description**
    /// Encode a dictionary
    ///**Parameters**
    ///  * `values` - parameter of type `Map<string, Value>`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let dict (values: Map<string, JsonNode>) =
        values
        |> Map.toList
        |> object

    let bigint (value: bigint) : JsonNode =
        JsonValue.Create(value.ToString(CultureInfo.InvariantCulture))

    let datetime (value : System.DateTime) : JsonNode =
        JsonValue.Create(value.ToString("O", CultureInfo.InvariantCulture))

    /// The DateTime is always encoded using UTC representation
    let datetimeOffset (value: System.DateTimeOffset) : JsonNode =
        JsonValue.Create(value.ToString("O", CultureInfo.InvariantCulture))

    let timespan (value: System.TimeSpan) : JsonNode =
        JsonValue.Create(value.ToString())


    let sbyte (value: sbyte) : JsonNode =
        JsonValue.Create(box value)

    let byte (value: byte) : JsonNode =
        JsonValue.Create(box value)

    let int16 (value: int16) : JsonNode =
        JsonValue.Create(box value)

    let uint16 (value: uint16) : JsonNode =
        JsonValue.Create(box value)

    let int (value: int) : JsonNode =
        JsonValue.Create(value)

    let uint32 (value: uint32) : JsonNode =
        JsonValue.Create(box value)

    let int64 (value: int64) : JsonNode =
        JsonValue.Create(value.ToString(CultureInfo.InvariantCulture))

    let uint64 (value: uint64) : JsonNode =
        JsonValue.Create(value.ToString(CultureInfo.InvariantCulture))

    let unit () : JsonNode =
        JsonValue.Create(null)

    let tuple2
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (v1, v2) : JsonNode =
        [| enc1 v1
           enc2 v2 |] |> array

    let tuple3
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (v1, v2, v3) : JsonNode =
        [| enc1 v1
           enc2 v2
           enc3 v3 |] |> array

    let tuple4
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (enc4 : Encoder<'T4>)
            (v1, v2, v3, v4) : JsonNode =
        [| enc1 v1
           enc2 v2
           enc3 v3
           enc4 v4 |] |> array

    let tuple5
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (enc4 : Encoder<'T4>)
            (enc5 : Encoder<'T5>)
            (v1, v2, v3, v4, v5) : JsonNode =
        [| enc1 v1
           enc2 v2
           enc3 v3
           enc4 v4
           enc5 v5 |] |> array

    let tuple6
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (enc4 : Encoder<'T4>)
            (enc5 : Encoder<'T5>)
            (enc6 : Encoder<'T6>)
            (v1, v2, v3, v4, v5, v6) : JsonNode =
        [| enc1 v1
           enc2 v2
           enc3 v3
           enc4 v4
           enc5 v5
           enc6 v6 |] |> array

    let tuple7
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (enc4 : Encoder<'T4>)
            (enc5 : Encoder<'T5>)
            (enc6 : Encoder<'T6>)
            (enc7 : Encoder<'T7>)
            (v1, v2, v3, v4, v5, v6, v7) : JsonNode =
        [| enc1 v1
           enc2 v2
           enc3 v3
           enc4 v4
           enc5 v5
           enc6 v6
           enc7 v7 |] |> array

    let tuple8
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (enc4 : Encoder<'T4>)
            (enc5 : Encoder<'T5>)
            (enc6 : Encoder<'T6>)
            (enc7 : Encoder<'T7>)
            (enc8 : Encoder<'T8>)
            (v1, v2, v3, v4, v5, v6, v7, v8) : JsonNode =
        [| enc1 v1
           enc2 v2
           enc3 v3
           enc4 v4
           enc5 v5
           enc6 v6
           enc7 v7
           enc8 v8 |] |> array

    let map (keyEncoder : Encoder<'key>) (valueEncoder : Encoder<'value>) (values : Map<'key, 'value>) : JsonNode =
        values
        |> Map.toList
        |> List.map (tuple2 keyEncoder valueEncoder)
        |> list

    ////////////
    // Enum ///
    /////////

    module Enum =

        let byte<'TEnum when 'TEnum : enum<byte>> (value : 'TEnum) : JsonNode =
            LanguagePrimitives.EnumToValue value
            |> byte

        let sbyte<'TEnum when 'TEnum : enum<sbyte>> (value : 'TEnum) : JsonNode =
            LanguagePrimitives.EnumToValue value
            |> sbyte

        let int16<'TEnum when 'TEnum : enum<int16>> (value : 'TEnum) : JsonNode =
            LanguagePrimitives.EnumToValue value
            |> int16

        let uint16<'TEnum when 'TEnum : enum<uint16>> (value : 'TEnum) : JsonNode =
            LanguagePrimitives.EnumToValue value
            |> uint16

        let int<'TEnum when 'TEnum : enum<int>> (value : 'TEnum) : JsonNode =
            LanguagePrimitives.EnumToValue value
            |> int

        let uint32<'TEnum when 'TEnum : enum<uint32>> (value : 'TEnum) : JsonNode =
            LanguagePrimitives.EnumToValue value
            |> uint32

    ///**Description**
    /// Convert a `Value` into a prettified string.
    ///**Parameters**
    ///  * `space` - parameter of type `int` - Amount of indentation
    ///  * `value` - parameter of type `obj` - Value to convert
    ///
    ///**Output Type**
    ///  * `string`
    ///
    ///**Exceptions**
    ///
    let toString (space: int) (token: JsonNode) : string =
        let mutable writerOptions = JsonWriterOptions()
        writerOptions.Indented <- space > 0

        use stream = new MemoryStream()
        use jsonWriter = new Utf8JsonWriter(stream, writerOptions)

        token.WriteTo(jsonWriter)

        Encoding.UTF8.GetString(stream.ToArray())

    //////////////////
    // Reflection ///
    ////////////////

    open FSharp.Reflection

    type private EncoderCrate<'T>(enc: Encoder<'T>) =
        inherit BoxedEncoder()
        override _.Encode(value: obj): JsonNode =
            enc (unbox value)
        member _.UnboxedEncoder = enc

    let boxEncoder (d: Encoder<'T>): BoxedEncoder =
        EncoderCrate(d) :> BoxedEncoder

    let unboxEncoder<'T> (d: BoxedEncoder): Encoder<'T> =
        (d :?> EncoderCrate<'T>).UnboxedEncoder

    let private (|StringifiableType|_|) (t: System.Type): (obj->string) option =
        let fullName = t.FullName
        if fullName = typeof<string>.FullName then
            Some unbox
        elif fullName = typeof<System.Guid>.FullName then
            Some(fun (v: obj) -> (v :?> System.Guid).ToString())
        else None

    let rec private autoEncodeRecordsAndUnions extra (caseStrategy : CaseStrategy) (skipNullField : bool) (t: System.Type) : BoxedEncoder =
        // Add the encoder to extra in case one of the fields is recursive
        let encoderRef = ref Unchecked.defaultof<_>
        let extra = extra |> Map.add t.FullName encoderRef
        let encoder =
            if FSharpType.IsRecord(t, allowAccessToPrivateRepresentation=true) then
                let setters =
                    FSharpType.GetRecordFields(t, allowAccessToPrivateRepresentation=true)
                    |> Array.map (fun fi ->
                        let targetKey = Util.Casing.convert caseStrategy fi.Name
                        let encoder = autoEncoder extra caseStrategy skipNullField fi.PropertyType
                        fun (source: obj) (target: JsonNode) ->
                            let value = FSharpValue.GetRecordField(source, fi)
                            if not skipNullField || (skipNullField && not (isNull value)) then // Discard null fields
                                target[targetKey] <- encoder.Encode value
                            target)
                boxEncoder(fun (source: obj) ->
                    (JsonObject() :> JsonNode, setters) ||> Seq.fold (fun target set -> set source target))
            elif FSharpType.IsUnion(t, allowAccessToPrivateRepresentation=true) then
                boxEncoder(fun (value: obj) ->
                    let info, fields = FSharpValue.GetUnionFields(value, t, allowAccessToPrivateRepresentation=true)
                    match fields.Length with
                    | 0 ->
                        #if !NETFRAMEWORK
                        match t with
                        // Replicate Fable behavior when using StringEnum
                        | Util.Reflection.StringEnum t ->
                            match info with
                            | Util.Reflection.CompiledName name -> string name
                            | _ ->
                                match t.ConstructorArguments with
                                | Util.Reflection.LowerFirst ->
                                    let name = info.Name[..0].ToLowerInvariant() + info.Name[1..]
                                    string name
                                | Util.Reflection.Forward -> string info.Name

                        | _ -> string info.Name
                        #else
                        string info.Name
                        #endif

                    | len ->
                        let fieldTypes = info.GetFields()
                        let target = Array.zeroCreate<JsonNode> (len + 1)
                        target[0] <- string info.Name
                        for i = 1 to len do
                            let encoder = autoEncoder extra caseStrategy skipNullField fieldTypes[i-1].PropertyType
                            target[i] <- encoder.Encode(fields[i-1])
                        array target)
            else
                failwith $"""Cannot generate auto encoder for %s{t.FullName}. Please pass an extra encoder.

Documentation available at: https://thoth-org.github.io/Thoth.Json/documentation/auto/extra-coders.html#ready-to-use-extra-coders"""
        encoderRef.Value <- encoder
        encoder

    and private genericSeq (encoder: BoxedEncoder) =
        boxEncoder(fun (xs: obj) ->
            let arr = JsonArray()
            for x in xs :?> System.Collections.IEnumerable do
                arr.Add(encoder.Encode(x))
            arr :> JsonNode)

    and private autoEncoder (extra: Map<string, ref<BoxedEncoder>>) caseStrategy (skipNullField : bool) (t: System.Type) : BoxedEncoder =
      let fullname = t.FullName
      match Map.tryFind fullname extra with
      | Some encoderRef -> boxEncoder(fun v -> encoderRef.contents.BoxedEncoder v)
      | None ->
        if t.IsArray then
            t.GetElementType() |> autoEncoder extra caseStrategy skipNullField |> genericSeq
        elif t.IsGenericType then
            if FSharpType.IsTuple(t) then
                let encoders =
                    FSharpType.GetTupleElements(t)
                    |> Array.map (autoEncoder extra caseStrategy skipNullField)
                boxEncoder(fun (value: obj) ->
                    FSharpValue.GetTupleFields(value)
                    |> Seq.mapi (fun i x -> encoders[i].Encode x) |> seq)
            else
                let fullname = t.GetGenericTypeDefinition().FullName
                if fullname = typedefof<obj option>.FullName then
                    // Evaluate lazily so we don't need to generate the encoder for null values
                    let encoder = lazy autoEncoder extra caseStrategy skipNullField t.GenericTypeArguments[0]
                    boxEncoder(fun (value: obj) ->
                        if isNull value then nil
                        else
                            let _, fields = FSharpValue.GetUnionFields(value, t, allowAccessToPrivateRepresentation=true)
                            encoder.Value.Encode fields[0])
                elif fullname = typedefof<obj list>.FullName
                    || fullname = typedefof<Set<string>>.FullName then
                    // I don't know how to support seq for now.
                    // || fullname = typedefof<obj seq>.FullName
                    t.GenericTypeArguments[0] |> autoEncoder extra caseStrategy skipNullField |> genericSeq
                elif fullname = typedefof< Map<string, obj> >.FullName then
                    let keyType = t.GenericTypeArguments[0]
                    let valueType = t.GenericTypeArguments[1]
                    let valueEncoder = valueType |> autoEncoder extra caseStrategy skipNullField
                    let kvProps = typedefof<KeyValuePair<obj,obj>>.MakeGenericType(keyType, valueType).GetProperties()
                    match keyType with
                    | StringifiableType toString ->
                        boxEncoder(fun (value: obj) ->
                            let target = JsonObject()
                            for kv in value :?> System.Collections.IEnumerable do
                                let k = kvProps[0].GetValue(kv)
                                let v = kvProps[1].GetValue(kv)
                                target[toString k] <- valueEncoder.Encode v
                            target :> JsonNode)
                    | _ ->
                        let keyEncoder = keyType |> autoEncoder extra caseStrategy skipNullField
                        boxEncoder(fun (value: obj) ->
                            let target = JsonArray()
                            for kv in value :?> System.Collections.IEnumerable do
                                let k = kvProps[0].GetValue(kv)
                                let v = kvProps[1].GetValue(kv)
                                target.Add(JsonArray([|keyEncoder.Encode k; valueEncoder.Encode v|]))
                            target :> JsonNode)
                else
                    autoEncodeRecordsAndUnions extra caseStrategy skipNullField t
        elif t.IsEnum then
            let enumType = System.Enum.GetUnderlyingType(t).FullName
            if enumType = typeof<sbyte>.FullName then
                boxEncoder sbyte
            elif enumType = typeof<byte>.FullName then
                boxEncoder byte
            elif enumType = typeof<int16>.FullName then
                boxEncoder int16
            elif enumType = typeof<uint16>.FullName then
                boxEncoder uint16
            elif enumType = typeof<int>.FullName then
                boxEncoder int
            elif enumType = typeof<uint32>.FullName then
                boxEncoder uint32
            else
                failwith
                    $"""Cannot generate auto encoder for %s{t.FullName}.
Thoth.Json.Net only support the following enum types:
- sbyte
- byte
- int16
- uint16
- int
- uint32
If you can't use one of these types, please pass an extra encoder.
                    """
        else
            if fullname = typeof<bool>.FullName then
                boxEncoder bool
            elif fullname = typeof<unit>.FullName then
                boxEncoder unit
            elif fullname = typeof<string>.FullName then
                boxEncoder string
            elif fullname = typeof<char>.FullName then
                boxEncoder char
            elif fullname = typeof<sbyte>.FullName then
                boxEncoder sbyte
            elif fullname = typeof<byte>.FullName then
                boxEncoder byte
            elif fullname = typeof<int16>.FullName then
                boxEncoder int16
            elif fullname = typeof<uint16>.FullName then
                boxEncoder uint16
            elif fullname = typeof<int>.FullName then
                boxEncoder int
            elif fullname = typeof<uint32>.FullName then
                boxEncoder uint32
            elif fullname = typeof<float>.FullName then
                boxEncoder float
            elif fullname = typeof<float32>.FullName then
                boxEncoder float32
            elif fullname = typeof<System.DateTime>.FullName then
                boxEncoder datetime
            elif fullname = typeof<System.DateTimeOffset>.FullName then
                boxEncoder datetimeOffset
            elif fullname = typeof<System.TimeSpan>.FullName then
                boxEncoder timespan
            elif fullname = typeof<System.Guid>.FullName then
                boxEncoder guid
            elif fullname = typeof<obj>.FullName then
                boxEncoder(fun (v: obj) -> JsonValue.Create(v) :> JsonNode)
            else
                autoEncodeRecordsAndUnions extra caseStrategy skipNullField t

    let private makeExtra (extra: ExtraCoders option) =
        match extra with
        | None -> Map.empty
        | Some e -> Map.map (fun _ (enc,_) -> ref enc) e.Coders

    module Auto =

        /// This API  is only implemented inside Thoth.Json.Net for now
        /// The goal of this API is to provide better interop when consuming Thoth.Json.Net from a C# project
        type LowLevel =
            /// ATTENTION: Use this only when other arguments (isCamelCase, extra) don't change
            static member generateEncoderCached<'T> (t: System.Type, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders, ?skipNullField: bool): Encoder<'T> =
                let caseStrategy = defaultArg caseStrategy PascalCase
                let skipNullField = defaultArg skipNullField true

                let key =
                    t.FullName
                    |> (+) (Operators.string caseStrategy)
                    |> (+) (extra |> Option.map (fun e -> e.Hash) |> Option.defaultValue "")

                let encoderCrate =
                    Cache.Encoders.Value.GetOrAdd(key, fun _ ->
                        autoEncoder (makeExtra extra) caseStrategy skipNullField t)

                fun (value: 'T) ->
                    encoderCrate.Encode value

    type Auto =
        /// ATTENTION: Use this only when other arguments (caseStrategy, extra) don't change
        static member generateEncoderCached<'T>(?caseStrategy : CaseStrategy, ?extra: ExtraCoders, ?skipNullField: bool): Encoder<'T> =
            let t = typeof<'T>
            Auto.LowLevel.generateEncoderCached(t, ?caseStrategy = caseStrategy, ?extra = extra, ?skipNullField=skipNullField)

        static member generateEncoder<'T>(?caseStrategy : CaseStrategy, ?extra: ExtraCoders, ?skipNullField: bool): Encoder<'T> =
            let caseStrategy = defaultArg caseStrategy PascalCase
            let skipNullField = defaultArg skipNullField true
            let encoderCrate = autoEncoder (makeExtra extra) caseStrategy skipNullField typeof<'T>
            fun (value: 'T) ->
                encoderCrate.Encode value

        static member toString(space : int, value : 'T, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders, ?skipNullField: bool) : string =
            let encoder = Auto.generateEncoder(?caseStrategy=caseStrategy, ?extra=extra, ?skipNullField=skipNullField)
            encoder value |> toString space

        static member toString(value : 'T, ?caseStrategy : CaseStrategy, ?extra: ExtraCoders, ?skipNullField: bool) : string =
            Auto.toString(0, value, ?caseStrategy=caseStrategy, ?extra=extra, ?skipNullField=skipNullField)

    ///**Description**
    /// Convert a `Value` into a prettified string.
    ///**Parameters**
    ///  * `space` - parameter of type `int` - Amount of indentation
    ///  * `value` - parameter of type `obj` - Value to convert
    ///
    ///**Output Type**
    ///  * `string`
    ///
    ///**Exceptions**
    ///
    [<System.Obsolete("Please use toString instead")>]
    let encode (space: int) (token: JsonNode) : string = toString space token

    ///**Description**
    /// Encode an option
    ///**Parameters**
    ///  * `encoder` - parameter of type `'a -> Value`
    ///
    ///**Output Type**
    ///  * `'a option -> Value`
    ///
    ///**Exceptions**
    ///
    let option (encoder : 'a -> JsonNode) =
        Option.map encoder >> Option.defaultWith (fun _ -> nil)
