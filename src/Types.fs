namespace Thoth.Json.Net

open System.Text.RegularExpressions

type JsonValue = Newtonsoft.Json.Linq.JToken

type ErrorReason =
    | BadPrimitive of string * JsonValue
    | BadPrimitiveExtra of string * JsonValue * string
    | BadType of string * JsonValue
    | BadField of string * JsonValue
    | BadPath of string * JsonValue * string
    | TooSmallArray of string * JsonValue
    | FailMessage of string
    | BadOneOf of string list

type CaseStrategy =
    | PascalCase
    | CamelCase
    | SnakeCase

type DecoderError = string * ErrorReason

type Decoder<'T> = string -> JsonValue -> Result<'T, DecoderError>

type Encoder<'T> = 'T -> JsonValue

[<AbstractClass>]
type BoxedDecoder() =
    abstract Decode: path : string * token: JsonValue -> Result<obj, DecoderError>
    member this.BoxedDecoder: Decoder<obj> =
        fun path token -> this.Decode(path, token)

[<AbstractClass>]
type BoxedEncoder() =
    abstract Encode: value: obj -> JsonValue
    member this.BoxedEncoder: Encoder<obj> = this.Encode

type ExtraCoders =
    { Hash: string
      Coders: Map<string, BoxedEncoder * BoxedDecoder> }

module internal Cache =
    open System
    open System.Collections.Concurrent

    type Cache<'Value>() =
        let cache = ConcurrentDictionary<string, 'Value>()
        member __.GetOrAdd(key: string, factory: string->'Value) =
            cache.GetOrAdd(key, factory)

    let Encoders = lazy Cache<BoxedEncoder>()
    let Decoders = lazy Cache<BoxedDecoder>()

module internal Util =

    module Casing =
        let lowerFirst (str : string) = str.[..0].ToLowerInvariant() + str.[1..]
        let convert caseStrategy fieldName =
            match caseStrategy with
            | CamelCase -> lowerFirst fieldName
            | SnakeCase -> Regex.Replace(lowerFirst fieldName, "[A-Z]","_$0").ToLowerInvariant()
            | PascalCase -> fieldName
