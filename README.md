# Simple Discord Subtitling Bot

Simple Discord Subtitling Bot is an application designed to transcribe voice communications from Discord in real-time and provide subtitles for better accessibility and convenience.

This application was created to assist a deaf friend of mine in conversing and socializing on Discord with his friends, and is a very primitive & small project.

## Features

- Real-time voice to text conversion for Discord voice channels.
- Accurate attribution of real-time subtitles to users within the voice chat.
- Overlay window displaying subtitles with adjustable parameters.

## Getting Started

### Prerequisites

Before running the Simple Discord Subtitling Bot, make sure you have the following:

- .NET Core 3.1 or later.
- A Discord bot token.
- Microsoft Cognitive Services API key for speech recognition.

## Obtaining a Discord Bot Token

To use the Simple Discord Subtitling Bot, you'll need a Discord Bot Token. Follow these steps to create a bot account and get your token:

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications).
2. Log in with your Discord account.
3. Click on the "New Application" button. Give your application a name and confirm the creation.
4. Navigate to the "Bot" tab on the left side panel and click on "Add Bot".
5. You will see a message asking for confirmation; proceed by clicking "Yes, do it!".
6. Once the bot is created, you'll see a token in the "Token" section. Click "Copy" to save your Discord Bot Token.
7. Remember to invite the bot to your Discord server by using the "OAuth2" tab, selecting the `bot` scope, and granting it the necessary permissions.

**Important**: Keep your token secure. Do not share it with others or expose it in public code repositories.

## Obtaining a Microsoft Cognitive Services API Key

For speech-to-text functionality, you will need an API Key from Microsoft Cognitive Services. Here's how to get one:

1. Go to the [Azure Portal](https://portal.azure.com/).
2. Create an account or sign in if you already have one.
3. Once logged in, search for "Speech Services" in the top search bar and create a new Speech Service resource.
4. Go through the creation process, filling out the necessary information such as subscription, resource group, region, and resource name.
5. After your resource is deployed, go to your resource page, and find the "Keys and Endpoint" section on the left panel.
6. Copy one of the provided keys; this will be your Microsoft Cognitive Services API Key.

**Note**: Microsoft may offer a free tier for this service, but you should review their pricing information to avoid any unexpected charges. I have tested this on the Standard subscription tier. There are some limitations on their free tier, so if something is going wrong, it might be worth switching to that. As of the date of publishing this application, Microsoft is offering 300 GBP to newly-created accounts.

Once you have both the Discord Bot Token and the Microsoft Cognitive Services API Key, enter them into the corresponding fields in the Simple Discord Subtitling Bot application.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) for Discord API wrapper.
- [Microsoft Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/) for speech recognition.

## Disclaimer

This bot is intended for educational and development purposes. Please adhere to Discord's terms of service while using it.
