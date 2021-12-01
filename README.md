# TricksterBots

This repository contains the source for for the _suggest_ methods of the Trickster Cards bots (computer players) along with a Web API driver for local testing.

## Requirements

- Visual Studio 2019 or 2022
- .NET Framework 4.8

## Getting Started

After installing the requirements and cloning this repository, open the Visual Studio solution file TricksterBots.sln. This will open a solution with 2 projects: TricksterBots and WebAPI.

Bot source is located in the Bots folder in the TricksterBots project. It is organized into game folders. Most games have only file file: *game*Bot.cs.

## Testing

Local testing is done using the WebAPI project. It contains Web API controllers that invoke the _suggest_ methods on a game's bot. Start this project -- generally with debugging -- and reference it with the bot=_origin_ query string parameter when running our test server at https://tricksterwest.azurewebsites.net/game/.
