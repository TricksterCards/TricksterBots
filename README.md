# TricksterBots

This repository contains the source for for the _suggest_ methods of the Trickster Cards bots (computer players) along with a Web API driver for local testing.

## Requirements

- .NET 8
- Visual Studio Code (optional, but recommended)
- C# Dev Kit Extension for VS Code (optional, but recommended)
- GitHub Desktop (optional, but recommended)

## Getting Started

Create and clone a fork of this repository from which to submit pull requests. If not using GitHub Desktop, ensure you also clone submodules. If you're not familiar with GitHub's pull request process you can learn about using GitHub Desktop to accomplish this here:

- [Cloning and Forking Repositories](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/adding-and-cloning-repositories/cloning-and-forking-repositories-from-github-desktop)
- [Creating a Pull Request](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/working-with-your-remote-repository-on-github-or-github-enterprise/creating-an-issue-or-pull-request#creating-a-pull-request)

After installing the requirements and cloning this repository, open the cloned folder in VS Code. Install the "C# Dev Kit" extension when prompted. This will open a solution with 3 projects: BridgeBidder, TestBots, and TricksterBots.

Bot source is located in the Bots folder in the TricksterBots project. It is organized into game folders. Most games have only file file: *game*Bot.cs.

Documentation on the classes and interfaces used by the bots can be viewed at https://www.trickstercards.com/home/help/BotClasses.html.

## Testing

Unit testing is done using the TestBots project. These are the tests that will run automatically against any pull requests. You can use the Testing tab in VS Code to run the full suite or individual tests on your own machine. 
