# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

## 12.0.0 - 2024-05-23

### Changed

* Upgrade to Newtonsoft.Json 13.0.1 to fix security vulnerability

## 11.0.0 - 2023-02-08

### Fixed

* Guard `decodeMaybeNull` to evaluates the `decoder` only if the value is not `null`

## 10.1.0 - 2023-02-01

### Changed

* Fix #51: Relax `Fable.Core` restriction to `>= 3` allowing use of Fable.Core 4

## 10.0.0 - 2022-12-04

### Changed

* Don't throw when union doesn't contain field values

## 9.0.0 - 2022-11-09

### Fixed

* BREAKING CHANGE: Encode `sbyte`, `byte`, `int16`, `uint16`, `uint32` using integer representation instead of decimal.

    `12u` is represented using `12` instead of `12.0`. I don't know why Newtonsoft.Json defaults to decimal representation for these types.
* Fix path when auto decoding unions
* Fix auto coders for nested anon records

### Added

* Add source link support.
* Add `Decode.map'` and `Encode.map` to support `Map<'Key, 'Value>`
* Add `Decode.datetimeUtc`, `Decode.datetimeLocal`
* Add `Encode.Auto.toString(value)` which is equivalent to `Encode.Auto.toString(0, value)`
* Add doc comment to `Decode.fromValue`, `Decode.fromString`, `Decode.unsafeFromString`
* Add support for `char`
* Add link to the "extra coders" section when coders fail for missing types information
* Add `Decode.andMap` allowing to decoder large objects incrementally

### Changed

* Capture `JsonException` instead of `JsonReaderException` this seems to cover more cases (by @PierreYvesR)

### Deprecated

* Mark `Decode.datetime` as deprecated

## 8.0.0 - 2022-01-05

### Changed

* BREAKING CHANGE: Represent `sbyte` using number instead of string.
* BREAKING CHANGE: Represent `byte` using number instead of string.
* BREAKING CHANGE: Represent `int16` using number instead of string.
* BREAKING CHANGE: Represent `uint16` using number instead of string.

## 7.1.0 - 2021-09-12

### Fixed

* Fix #42: Force Newtonsoft to consider the whole JSON at once and not token per token

## 7.0.0 - 2021-09-12

### Removed

* PR #24: Remove broken Converter, people can use https://github.com/DnnFable/Thoth.Json.Net.Formatter as a replacement (by @SCullman)

## 6.0.0 - 2021-09-12

### Changed

* PR #44: Port API change from Thoth.Json [v6.0.0] Expose the helpers module so it can be used for custom decoders (by @weslenng)
* PR #43: Port API change from Thoth.Json [v5.1.0] Improve tree shaking for longs (by @weslenng)

### Fixed

* Fix #30: Fix Decode.Auto support for StringEnum

## 5.0.0 - 2020-10-15

### Fixed

* PR #39: optionalAt now returns `Ok None` even when the path does not exist (by @rommsen)

## 4.0.0 - 2020-03-04

### Changed

* isCamelCase is now replaced by caseStrategy=CamelCase

### Added

* Added caseStrategy that accept CamelCase | PascalCase | SnakeCase

## 3.6.0 - 2019-10-24

### Added

* Add supports for `byte`
* Add supports for `sbyte`
* Add supports for `int16>`
* Add supports for `uint16`
* Add supports for `uint32`
* Add supports for `float32`
* Add supports for `enum<byte>`
* Add supports for `enum<sbyte>`
* Add supports for `enum<int16>`
* Add supports for `enum<uint16>`
* Add supports for `enum<int>`
* Add supports for `enum<uint32>`
* Add support for `unit`
* Allow to configure if `null` field should be omitted or no. Set `skipNullField` to `false` when using auto encoder, to include `myField: null` in your json output

### Changed

* Fix #18: Remove the cache limitation when using generateDecoderCached (by &SCullman)
* Fix Encode.decimal comment (by @alfonsogarciacaro)

## 3.5.1 - 2019-06-24
### Changed

* Release stable version

## 3.5.1-beta-001 - 2019-06-24
### Changed

* Stop using first person when reporting an error. Related to https://github.com/thoth-org/Thoth.Json/issues/19
* Use `dotnet pack` to generate the package
* Add upper restriction to `Fable.Core`

## 3.5.0 - 2019-06-24
### Changed

* Stop using first person when reporting an error. Related to https://github.com/thoth-org/Thoth.Json/issues/19

## 3.5.0-beta-001 - 2019-06-07
### Changed

* Use `paket.template` to generate the nupkg. The goal is to have "correct" dependencies handling

## 3.4.0 - 2019-06-07
### Added

* `Decode.Auto.LowLevel` providing better interop when consuming Thoth.Json.Net from a C# project
* `Encode.Auto.LowLevel` providing better interop when consuming Thoth.Json.Net from a C# project
* (Json)Converter, this is needed for people using Asp.Net WebApi (by @SCullman)

### Fixed

* Fix #11: Replicate Fable behaviour when encoding `StringEnum`. Only when not on NETFRAMEWORK!!!

### Changed

* Ensure that Thoth.Json.Net references the minimum allowed version of Newtonsoft.Json (by @bentayloruk)

## 3.3.0 - 2019-06-05
### Fixed

* Fix #12: Support auto coders with recursive types (by @alfonsogarciacaro)

## 3.2.0 - 2019-05-14
### Fixed

* Fix #13: Decode.string fails on strings with datetime

## 3.1.0 - 2019-05-03
### Fixed

* Fix auto encoder when generating an optinal unkown type and the runtime value is `None`

## 3.0.0 - 2019-04-17
### Changed

* Release stable version

## 3.0.0-beta-005 - 2019-04-10
### Changed

* Lower requested version of Newtonsoft to `>=11.0.2`

## 3.0.0-beta-004 - 2019-04-10

## 3.0.0-beta-003 - 2019-04-02
### Fixed
* Fix Decode.oneOf in combination with object builder (by @alfonsogarciacaro)
* Make `Decode.field` consistant and report the exact error
* Make `Decode.at` bconsistant and report the exact error

### Changed
* Make Decode.object output 1 error or all the errors if there are severals
* Improve BadOneOf errors readibility

## 3.0.0-beta-002 - 2019-01-11
### Added

* Adding `TimeSpan` support (by @rfrerebe)

## 3.0.0-beta-001 - 2019-01-10
### Added

* Add `Set` support in auto coders (by @alfonsogarciacaro)
* Add `extra` support to auto coders. So people can now override/extends auto coders capabilities (by @alfonsogarciacaro)

### Changed

* Use reflection for auto encoders just as auto decoders. This will help keep the JSON representatin in synx between manual and auto coders (by @alfonsogarciacaro)
* `Decode.datetime` always outputs universal time (by @alfonsogarciacaro)
* If a coder is missing, auto coders will fail on generation phase instead of coder evaluation phase (by @alfonsogarciacaro)
* By default `int64` - `uint64` - `bigint` - `decimal` support is being disabled from auto coders to reduce bundle size (by @alfonsogarciacaro)

### Removed

* Mark `Decode.unwrap` as private. It's now only used internally for object builder. This will encourage people to use `Decode.fromValue`.

## 2.5.0 - 2018-11-08
### Added

* Make auto decoder support record/unions with private constructors

## 2.4.0 - 2018-11-07
### Changed

* Make auto decoder succeeds on Class marked as optional

## 2.3.0 - 2018-10-18
### Changed

* Added CultureInfo.InvariantCulture to all Encoder functions where it was possible (by @draganjovanovic1)

### Fixed

* Fix #59: Make auto decoder support optional fields when missing from JSON
* Fix #61: Support object keys with JsonPath characters when using `Decode.dict`
* Fix #51: Add support for `Raw` decoder in object builders

## 2.2.0 - 2018-10-11
### Added

* Re-add optional and optionalAt related to #51

### Fixed

* Various improvements for Primitive types improvements  (by @draganjovanovic1)
* Fix decoding of optional fields (by @eugene-g)

## 2.1.0 - 2018-10-3
### Fixed

* Fix nested object builder (ex: get.Optional.Field > get.Required.Field)
* Fix exception handling

## 2.0.0 - 2018-10-01
### Added

* Release stable

## 2.0.0-beta-004 - 2018-08-03
### Added

* Add Encoders for all the equivalent Decoders

## 2.0.0-beta-003 - 2018-07-16
### Changed

* Make auto decoder safe by default

## 2.0.0-beta-002 - 2018-07-12
### Fixed

* Fix `Decode.decodeString` signature

## 2.0.0-beta-001 - 2018-07-11
### Added

* Support auto decoders and encoders
* Add object builder style for the decoders

### Changed

* Better error, by now tracking the path

### Deprecated

* Mark `Encode.encode`, `Decode.decodeString`, `Decode.decodeValue` as obsoletes

### Removed

* Remove pipeline style for the decoders

## 1.1.0 - 2018-06-08
### Fixed

* Ensure that `field` `at` `optional` `optionalAt` works with object

## 1.0.1 - 2018-05-30
### Fixed

* A float from int works

## 1.0.0 - 2018-04-17
### Added

* Initial release
