# TricksterBots

This repository contains the source for for the _suggest_ methods of the Trickster Cards bots (computer players) along with a Web API driver for local testing.

## Requirements

- Visual Studio 2022
- .NET Framework 4.8
- GitHub Desktop (optional, but recommended)

## Getting Started

Create and clone a fork of this repository from which to submit pull requests. If you're not familiar with GitHub's pull request process
you can learn about using GitHub Desktop to accomplish this here:

- [Cloning and Forking Repositories](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/adding-and-cloning-repositories/cloning-and-forking-repositories-from-github-desktop)
- [Creating a Pull Request](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/working-with-your-remote-repository-on-github-or-github-enterprise/creating-an-issue-or-pull-request#creating-a-pull-request)

After installing the requirements and cloning this repository, open the Visual Studio solution file TricksterBots.sln. This will open a solution with 2 projects: TricksterBots and WebAPI.

Bot source is located in the Bots folder in the TricksterBots project. It is organized into game folders. Most games have only file file: *game*Bot.cs.

Documentation on the classes and interfaces used by the bots can be viewed at https://www.trickstercards.com/home/help/BotClasses.html.

## Testing

Local testing is done using the WebAPI project. It contains Web API controllers that invoke the _suggest_ methods on a game's bot. Start this project -- generally with debugging -- and click the link on the top line of the **Trickster Web Bot API** page displayed.

To watch suggestion activity, open the browser's dev tools, and display the console. Filter the messages with "Bot suggested" and you'll see only the messages for the bot calls.
