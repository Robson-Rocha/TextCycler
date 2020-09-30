# <img src="docs/textcycle-nuget.png" style="zoom:25%;" />  TextCycler 
TextCycler is a simple command line tool for cycling texts and perform some sequence and time replacements on the text, useful for OBS Streamings, or in any application which you can read the value of a text file (and is capable of detecting the changes of the text file).

## Requirements

TextCycler is a .NET Core Global Tool, and as such, can be used in any platform where you can install .NET Core 3.1 (Windows, MacOS and Linux).
To install .NET Core 3.1, [click here](https://dotnet.microsoft.com/download/dotnet-core/3.1).

## Installation

To install TextCycler, in your preferred terminal, use the following command:

```bash
dotnet tool install --global TextCycler --version 1.0.1
```

See the releases section for the current version, or symply check at NuGet.org for the current version [clicking here](https://www.nuget.org/packages/TextCycler).

## Usage

### Creating the JSON configuration file

After installation, and before you can use TextCycler, you'll need to create a JSON configuration file. You can create a starter template with the following command in your preferred terminal:

```bash
textcycler -c config.json --generateConfig
```

This will create a config.json file in the current directory. You can specify the full path of the config.json in the `-c` or `--configFile` option, and it can have whatever name you prefer. Think of this config file as a 'Profile', in which you can have multiple profiles for different occasions.

The initial configuration file resembles the following:

```json
{
 "texts": [
  "First text",
  "Second text, with Current Time (#TIME#), and Rounded to Nearest 5 Minutes Time (#NTIME#)",
  "Third Text, with the sequence 0 (#SEQUENCE_00#) from 01 to 05",
  "Fourth Text, with the sequence 1 (#SEQUENCE_01#) using text values"
 ],
 "sequences": [
  [
   "01",
   "02",
   "03",
   "04",
   "05"
  ],
  [
   "lorem",
   "ipsum",
   "dolor"
  ]
 ],
 "targetFile": "targetfile.txt"
}
```

### Setting your texts in the configuration file

TextCycler works setting the text in one of the array entries in the `"texts"` section into the file specified in the `"targetFile"` section, each time you use the following command:

```bash
textcycler -c config.json
```

Each text must be into a string into the array, and you should escape unsupported JSON characters accordingly.

In the above command, the `-c` or `--configFile` option indicates the path to the config file.

TextCycler will create a  `"nextTextIndex"` section in the config file, that determines which text will be written the next time you execute TextCycler, starting with `0` for the first text and `n-1` for the nth text, and will be automatically be updated by TextCycler at each execution. When the last text is written, it will reset to `0`.

This way, given the above configuration files, executing TextCycler three times will output the following texts to the `targetFile` after each execution:

`First text`

`Second text, with Current Time (19:03), and Rounded to Nearest 5 Minutes Time (19:05)`

`Third Text, with the counter 0 (01) from 1 to 5`

We will discuss about the replacement tokens for time and counters shortly.

#### Overriding `nextTextIndex` 

If you want to write a specific text index to the `targetFile`, you can use the `-i` or `--index` option to specify the desired text index:

```bash
textcycler -c config.json -i 1
```

Which, given the above configuration file, will output the following text to the `targetFile`:

`Second text, with Current Time (19:03), and Rounded to Nearest 5 Minutes Time (19:05)`

Overriding the text index does not change the next text index.

#### Overriding `targetFile`

If you want to override the targetFile section, or simply prefer to provide its path in the command line, you can use the `-f` or  `--targetFile` option:

```bash
textcycler -c config.json -f somefile.txt
```

### Using Sequences

Sequences are a useful feature which allow you to use sequences of text, which will be applied in sequentially each time a text is written to the target file.

The configuration file shown above depicts a `"sequences"` section, which consists of an array of text arrays. Each text array represents the texts that will be replaced in the text, using a special **`##SEQUENCE_NN##`** sequence replacement token in the text, where `NN` stands for the index of the sequence value. As each index always start with zero, you can provide up to 100 values for each sequence. The values of a sequence can be any text.

For example, the third text in the above configuration file includes the `##SEQUENCE_00##` replacement token, which represents the value for the first sequence of values. If you execute the command below three times, to write specifically the third text to the target file:

```bash
textcycler -c config.json -i 3
```

It will produce the following texts after each time the TextCycler is executed:

`Third Text, with the sequence 0 (01) from 01 to 05`

`Third Text, with the sequence 0 (02) from 01 to 05`

`Third Text, with the sequence 0 (03) from 01 to 05`

The same will apply if you simply cycle through all the texts. Each time that the `##SEQUENCE_00##` is replaced, it will get the next value in the sequence, going back to the first value after the last value is replaced.

It is important to note that the sequence cycling occurs only after all the replacements. So using the same replacement token multiple times in the same text will produce the same value.

To control the current index of the sequences, TextCycler will create a `"sequencePositions"` in the config file. This section will be automatically recreated if you change the number of sequences in your config file.

#### Overriding a sequence value

You can override a sequence value using the `-s` or `--sequenceValue` option, providing the index of the sequence to be overriden and the value to override, separated by a comma, as in the following example:

```bash
textcycler -c config.json -i 3 -s 0,99
```

Which will produce the following text:

`Third Text, with the sequence 0 (99) from 01 to 05`

Overriding a sequence value does not change the next index for the sequence.

### Using Time Replacement Tokens

Time Replacement Tokens are useful to output the current time into the written text. 

There are two time basic replacement tokens: **`##TIME##`**, which outputs the current time in the 24 hours format, and **`##TIME12##`**, which outputs the current time in 12 hours format (including AM/PM).

If you want the time to be rounded to the nearest 5 minutes, you can use the **`##NTIME##`** and **`##NTIME12##`** variants.

#### Adding or subtracting minutes to the time

You can also add or subtract minutes to the time, using the **`##TIME+XX##`**, **`##TIME12+XX##`**, **`##NTIME+XX##`**, **`##NTIME12+XX##`**, **`##TIME-XX##`**, **`##TIME12-XX##`**, **`##NTIME-XX##`** and **`##NTIME12-XX##`**,  variants, where `XX` stands for the number of minutes to add or subtract, from 0 to 99 minutes.

It is useful in situations when you must consider a different time zone, or wants to generate a message like "Live Transmission will start at 19:00!", based on your computer time.

You can even combine the Time Replacement Tokens with the Sequence Replacement Tokens, i.e. **`##NTIME+##SEQUENCE_00####`**, where the number of minutes to be added comes from the replacement token **##SEQUENCE_00##**. Pay attention to the hash count, every replacement token starts with two hashes and end with another two, and that the sequence value must be numeric, or else the time replacement token could not be recognized.

#### Overriding the current time

If you do not want to use the current time, but set a specific time instead, you can use the `-t` or `--time` to set the specific time:

```bash
textcycler -c config.json -i 1 -t 19:00
```

The time can also be set in 12 hours format:

```bash
textcycler -c config.json -i 1 -t 7:00PM
```

