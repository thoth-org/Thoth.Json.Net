namespace Thoth.Json.Net

[<RequireQualifiedAccess>]
module Encode =

    open System.Globalization
    open System.Collections.Generic
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open System.IO

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
    let string (value : string) : JsonValue =
        JValue(value) :> JsonValue

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
    let guid (value : System.Guid) : JsonValue =
        value.ToString() |> string

    ///**Description**
    /// Encode an int
    ///
    ///**Parameters**
    ///  * `value` - parameter of type `int`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let int (value : int) : JsonValue =
        JValue(value) :> JsonValue

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
    let float (value : float) : JsonValue =
        JValue(value) :> JsonValue

    ///**Description**
    /// Encode a Decimal. (Currently decimal gets converted to float.)
    ///
    ///**Parameters**
    ///  * `value` - parameter of type `decimal`
    ///
    ///**Output Type**
    ///  * `Value`
    ///
    ///**Exceptions**
    ///
    let decimal (value : decimal) : JsonValue =
        JValue(value.ToString(CultureInfo.InvariantCulture)) :> JsonValue

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
    let nil : JsonValue =
        JValue.CreateNull() :> JsonValue

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
    let bool (value : bool) : JsonValue =
        JValue(value) :> JsonValue

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
    let object (values : (string * JsonValue) list) : JsonValue =
        values
        |> List.map (fun (key, value) ->
            JProperty(key, value)
        )
        |> JObject :> JsonValue

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
    let array (values : JsonValue array) : JsonValue =
        JArray(values) :> JsonValue

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
    let list (values : JsonValue list) : JsonValue =
        JArray(values) :> JsonValue

    let seq (values : JsonValue seq) : JsonValue =
        JArray(values) :> JsonValue

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
    let dict (values : Map<string, JsonValue>) =
        values
        |> Map.toList
        |> object

    let bigint (value : bigint) : JsonValue =
        JValue(value.ToString(CultureInfo.InvariantCulture)) :> JsonValue

    let int64 (value : int64) : JsonValue =
        JValue(value.ToString(CultureInfo.InvariantCulture)) :> JsonValue

    let uint32 (value : uint32) : JsonValue =
        JValue(value) :> JsonValue

    let uint64 (value : uint64) : JsonValue =
        JValue(value.ToString(CultureInfo.InvariantCulture)) :> JsonValue

    let datetime (value : System.DateTime) : JsonValue =
        JValue(value.ToString("O", CultureInfo.InvariantCulture)) :> JsonValue

    let datetimeOffset (value : System.DateTimeOffset) : JsonValue =
        JValue(value.ToString("O", CultureInfo.InvariantCulture)) :> JsonValue

    let timespan (value : System.TimeSpan) : JsonValue =
        JValue(value.ToString()) :> JsonValue

    let tuple2
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (v1, v2) : JsonValue =
        [| enc1 v1
           enc2 v2 |] |> array

    let tuple3
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (v1, v2, v3) : JsonValue =
        [| enc1 v1
           enc2 v2
           enc3 v3 |] |> array

    let tuple4
            (enc1 : Encoder<'T1>)
            (enc2 : Encoder<'T2>)
            (enc3 : Encoder<'T3>)
            (enc4 : Encoder<'T4>)
            (v1, v2, v3, v4) : JsonValue =
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
            (v1, v2, v3, v4, v5) : JsonValue =
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
            (v1, v2, v3, v4, v5, v6) : JsonValue =
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
            (v1, v2, v3, v4, v5, v6, v7) : JsonValue =
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
            (v1, v2, v3, v4, v5, v6, v7, v8) : JsonValue =
        [| enc1 v1
           enc2 v2
           enc3 v3
           enc4 v4
           enc5 v5
           enc6 v6
           enc7 v7
           enc8 v8 |] |> array

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
    let toString (space: int) (token: JsonValue) : string =
        let format = if space = 0 then Formatting.None else Formatting.Indented
        use stream = new StringWriter(NewLine = System.Environment.NewLine)
        use jsonWriter = new JsonTextWriter(
                                stream,
                                Formatting = format,
                                Indentation = space )

        token.WriteTo(jsonWriter)
        stream.ToString()

    //////////////////
    // Reflection ///
    ////////////////

    open FSharp.Reflection

    type private EncoderCrate<'T>(enc: Encoder<'T>) =
        inherit BoxedEncoder()
        override __.Encode(value: obj): JsonValue =
            enc (unbox value)
        member __.UnboxedEncoder = enc

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

    let rec private autoEncodeRecordsAndUnions extra (isCamelCase : bool) (t: System.Type) : BoxedEncoder =
        if FSharpType.IsRecord(t, allowAccessToPrivateRepresentation=true) then
            let setters =
                FSharpType.GetRecordFields(t, allowAccessToPrivateRepresentation=true)
                |> Array.map (fun fi ->
                    let targetKey =
                        if isCamelCase then fi.Name.[..0].ToLowerInvariant() + fi.Name.[1..]
                        else fi.Name
                    let encoder = autoEncoder extra isCamelCase fi.PropertyType
                    fun (source: obj) (target: JObject) ->
                        let value = FSharpValue.GetRecordField(source, fi)
                        if not(isNull value) then // Discard null fields
                            target.[targetKey] <- encoder.Encode value
                        target)
            boxEncoder(fun (source: obj) ->
                (JObject(), setters) ||> Seq.fold (fun target set -> set source target) :> JsonValue)
        elif FSharpType.IsUnion(t, allowAccessToPrivateRepresentation=true) then
            boxEncoder(fun (value: obj) ->
                let info, fields = FSharpValue.GetUnionFields(value, t, allowAccessToPrivateRepresentation=true)
                match fields.Length with
                | 0 -> string info.Name
                | len ->
                    let fieldTypes = info.GetFields()
                    let target = Array.zeroCreate<JsonValue> (len + 1)
                    target.[0] <- string info.Name
                    for i = 1 to len do
                        let encoder = autoEncoder extra isCamelCase fieldTypes.[i-1].PropertyType
                        target.[i] <- encoder.Encode(fields.[i-1])
                    array target)
        else
            failwithf "Cannot generate auto encoder for %s. Please pass an extra encoder." t.FullName

    and private genericSeq (encoder: BoxedEncoder) =
        boxEncoder(fun (xs: obj) ->
            let ar = JArray()
            for x in xs :?> System.Collections.IEnumerable do
                ar.Add(encoder.Encode(x))
            ar :> JsonValue)

    and private autoEncoder (extra: ExtraCoders) isCamelCase (t: System.Type) : BoxedEncoder =
      let fullname = t.FullName
      match Map.tryFind fullname extra with
      | Some(encoder,_) -> encoder
      | None ->
        if t.IsArray then
            t.GetElementType() |> autoEncoder extra isCamelCase |> genericSeq
        elif t.IsGenericType then
            if FSharpType.IsTuple(t) then
                let encoders =
                    FSharpType.GetTupleElements(t)
                    |> Array.map (autoEncoder extra isCamelCase)
                boxEncoder(fun (value: obj) ->
                    FSharpValue.GetTupleFields(value)
                    |> Seq.mapi (fun i x -> encoders.[i].Encode x) |> seq)
            else
                let fullname = t.GetGenericTypeDefinition().FullName
                if fullname = typedefof<obj option>.FullName then
                    let encoder = t.GenericTypeArguments.[0] |> autoEncoder extra isCamelCase
                    boxEncoder(fun (value: obj) ->
                        if isNull value then nil
                        else
                            let _, fields = FSharpValue.GetUnionFields(value, t, allowAccessToPrivateRepresentation=true)
                            encoder.Encode fields.[0])
                elif fullname = typedefof<obj list>.FullName
                    || fullname = typedefof<Set<string>>.FullName then
                    t.GenericTypeArguments.[0] |> autoEncoder extra isCamelCase |> genericSeq
                elif fullname = typedefof< Map<string, obj> >.FullName then
                    let keyType = t.GenericTypeArguments.[0]
                    let valueType = t.GenericTypeArguments.[1]
                    let valueEncoder = valueType |> autoEncoder extra isCamelCase
                    let kvProps = typedefof<KeyValuePair<obj,obj>>.MakeGenericType(keyType, valueType).GetProperties()
                    match keyType with
                    | StringifiableType toString ->
                        boxEncoder(fun (value: obj) ->
                            let target = JObject()
                            for kv in value :?> System.Collections.IEnumerable do
                                let k = kvProps.[0].GetValue(kv)
                                let v = kvProps.[1].GetValue(kv)
                                target.[toString k] <- valueEncoder.Encode v
                            target :> JsonValue)
                    | _ ->
                        let keyEncoder = keyType |> autoEncoder extra isCamelCase
                        boxEncoder(fun (value: obj) ->
                            let target = JArray()
                            for kv in value :?> System.Collections.IEnumerable do
                                let k = kvProps.[0].GetValue(kv)
                                let v = kvProps.[1].GetValue(kv)
                                target.Add(JArray [|keyEncoder.Encode k; valueEncoder.Encode v|])
                            target :> JsonValue)
                else
                    autoEncodeRecordsAndUnions extra isCamelCase t
        else
            if fullname = typeof<bool>.FullName then
                boxEncoder bool
            elif fullname = typeof<string>.FullName then
                boxEncoder string
            elif fullname = typeof<int>.FullName then
                boxEncoder int
            elif fullname = typeof<uint32>.FullName then
                boxEncoder uint32
            elif fullname = typeof<float>.FullName then
                boxEncoder float
            // These number types require extra libraries in Fable. To prevent penalizing
            // all users, extra encoders (withInt64, etc) must be passed when they're needed.

            // elif fullname = typeof<int64>.FullName then
            //     boxEncoder int64
            // elif fullname = typeof<uint64>.FullName then
            //     boxEncoder uint64
            // elif fullname = typeof<bigint>.FullName then
            //     boxEncoder bigint
            // elif fullname = typeof<decimal>.FullName then
            //     boxEncoder decimal
            elif fullname = typeof<System.DateTime>.FullName then
                boxEncoder datetime
            elif fullname = typeof<System.DateTimeOffset>.FullName then
                boxEncoder datetimeOffset
            elif fullname = typeof<System.TimeSpan>.FullName then
                boxEncoder timespan
            elif fullname = typeof<System.Guid>.FullName then
                boxEncoder guid
            elif fullname = typeof<obj>.FullName then
                boxEncoder(fun (v: obj) -> JValue(v) :> JsonValue)
            else
                autoEncodeRecordsAndUnions extra isCamelCase t

    type Auto =
        /// ATTENTION: Use this only when other arguments (isCamelCase, extra) don't change
        static member generateEncoderCached(t: System.Type, ?isCamelCase : bool, ?extra: ExtraCoders): Encoder<obj> =
            Cache.Encoders.Value.GetOrAdd(t, fun t ->
                let isCamelCase = defaultArg isCamelCase false
                let extra = match extra with Some e -> e | None -> Map.empty
                autoEncoder extra isCamelCase t).BoxedEncoder

        static member generateEncoderCached<'T>(?isCamelCase : bool, ?extra: ExtraCoders): Encoder<'T> =
            let t = typeof<'T>
            let encoderCrate =
                Cache.Encoders.Value.GetOrAdd(t, fun t ->
                    let isCamelCase = defaultArg isCamelCase false
                    let extra = match extra with Some e -> e | None -> Map.empty
                    autoEncoder extra isCamelCase t)
            fun (value: 'T) ->
                encoderCrate.Encode value

        static member generateEncoder<'T>(?isCamelCase : bool, ?extra: ExtraCoders): Encoder<'T> =
            let isCamelCase = defaultArg isCamelCase false
            let extra = match extra with Some e -> e | None -> Map.empty
            let encoderCrate = autoEncoder extra isCamelCase typeof<'T>
            fun (value: 'T) ->
                encoderCrate.Encode value

        static member toString(space : int, value : 'T, ?isCamelCase : bool, ?extra: ExtraCoders) : string =
            let encoder = Auto.generateEncoder(?isCamelCase=isCamelCase, ?extra=extra)
            encoder value |> toString space

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
    let encode (space: int) (token: JsonValue) : string = toString space token

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
    let option (encoder : 'a -> JsonValue) =
        Option.map encoder >> Option.defaultWith (fun _ -> nil)
