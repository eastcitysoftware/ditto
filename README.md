<div align="center">

![ditto](https://github.com/eastcitysoftware/ditto/blob/assets/ditto.png?raw=true)

[![build](https://github.com/eastcitysoftware/ditto/actions/workflows/build.yml/badge.svg)](https://github.com/eastcitysoftware/ditto/actions/workflows/build.yml)
![License](https://img.shields.io/github/license/eastcitysoftware/ditto)

Build websites with [TOML](https://github.com/prozolic/CsToml) & [Mustache](https://github.com/StubbleOrg/Stubble) templates.
</div>

---

Ditto is a static website generator that uses TOML for configuration and Mustache for templating. It provides a direct, data-driven approach to building websites. Mustache templates allow you to create dynamic content with ease, while TOML configuration files keep your settings organized and readable.

Because life is too short for complex website setups.

---

## Usage

```shell
Description:
  ditto, static webite generator with hot reload

Usage:
  Ditto <input> [options]

Arguments:
  <input>  The absolute path to website directory containing website.toml

Options:
  -?, -h, --help         Show help and usage information
  --version              Show version information
  output, -o (REQUIRED)  The output directory for the generated files
```
