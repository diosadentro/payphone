# Party Payphone

The Party Payphone project aims to make an interactive phone number that can be configured with a landline to auto dial. The Party Payphone can fulfill the following requests:

1. Add a song to a configured user's active Spotify Queue.
2. Change light devices via the Hubitat Maker API
3. Tell a joke
4. Record a message for a future caller
5. Redirect the caller to a number of different prank call numbers (such as the rejection hotline or rick-roll number)
6. Repeat the number

The system also provides an admin mode by pressing "*" which allows for the entry of an access code to enable dial out. This helps to bypass the autodialer hooked up to my payphone.

https://github.com/diosadentro/payphone/assets/3356337/e1cee0a8-4347-43e0-b6fa-d511feca5d6a

## Voice creation

The project utilizes [FakeYou.com](https://fakeyou.com/) to create deep fakes of different characters for playback during the call. These audio files are served out of an S3 bucket. To add your own audio files, name them in the following pattern:

- S3 Bucket
-- character_name
--- intro.wav - Intro sound like in the above audio
--- lights.wav - Message when change lights request is made
--- song-input.wav - Message to prompt to enter a song name and artist
--- select-song.wav - Message prompting the user to press the number to match a song
--- different-song.wav - Message when a song isn't found to prompt to search to search again
--- after-beep.wav - Prompt for user to record a message
--- save-or-record.wav - Prompt for the user to accept or re-record their message
--- recording-saved.wav - Message that the recording has been saved
--- joke1.wav - A joke to tell the caller
--- sorry.wav - A generic error message apologizing for the problem.

There will also need to be a general file with a beep sound for prompts:
- S3 Bucket
-- general
--- beep.wav - Some beep noise for song selection.

The bucket name is defined in the Environment Variable `AWS__BucketName`. The character_name is defined in the `Characters` Environment Variable as a comma separated list. For example: `"Characters": "arnold, barbie, bill-clinton, billy-mays, donald-trump, eminem, eric-cartman, hank-hill, hillary-clinton, morgan-freeman, quagmire, spongebob, stewie"`.

## Environment Variables

The following environment variables are required for functionality:

| Env Variable | Description | Example |
| ------------ | ------------- | ------- |
| AWS__AccessKey | Access key for account with permission to read S3 audio objects | abc123 |
| AWS__BucketName | Bucket name that holds S3 audio files | s3-audio-bucket |
| AWS__Region | AWS region | us-east-1 |
| AWS__SecretKey | AWS Secret key for account with permission to read S3 audio objects | abc123 |
| Characters | Comma separated list of deep-fake characters. These should match the top level directory names in S3 | arnold, spongebob, hillary-clinton |
| Database__Database | Mongodb name | payphone |
| Database__DatabaseType | Was eventually going to add support for different databases. Right now just Mongo is supported | Mongo |
| Database__Password | Database Service Account Password | abc123 |
| Database__Port | Database Port (27017) | 27017 |
| Database__Server | FQDN of Mongo database | database.fqdn.com |
| Database__Username | Database Service Account Username | abc123 |
| DisableAuth | Used to temporarily disable Twilio Signature Verification for testing | "true"/"false" |
| GlobalSettingsGuid | GUID for global settings Mongo object in the global collection. You can put any GUID here and it will be automatically generated on first run | b4083614-594c-465f-b6c3-92f659e6fbe4 |
| Hubitat__AuthToken | Auth token used by Hubitat Maker API | abc123 |
| Hubitat__DeviceIds | Comma separated list of device Ids for the Hubitat Maker API| 123, 456, 789 |
| Hubitat__Host | IP or hostname of Hubitat Maker API | Maker API hostname (note, code needs to be modified to use different ID in url) |
| Spotify__ClientId | Client ID for Spotify App | abc123 |
| Spotify__ClientSecret | Client Secret for Spotify App | abc123 |
| Spotify__RedirectUrl | Redirect URL for oauth2 if you'd like to use the /spotify post endpoint to get refresh token | https://redirect.com |
| Spotify__RefreshToken | Refresh token to use to obtain access tokens | abc123 |
| SurpriseNumbers | Comma separated list of prank phone numbers to connect the call to. Like rick roll hotline or rejection hotline | 123-456-7890, 789-012-3456 |
| Twilio__AccessCode | Access code for Twilio Account | abc123 |
| Twilio__AccountSid | Account sid for Twilio Account | abc123 |
| Twilio__AuthToken | Auth token for Twilio Account | abc123 |

## Deployment

The project utilizes Helm for deployment. See the `infrastructure/helm directory for details`. Make sure to update `<docker repo>` and `<cloudflare hostname>` in the values.yaml file. By default, the helm chart will create 3 replicas.

This project utilizes [Cloudflare Tunnel](https://www.cloudflare.com/products/tunnel/) to expose the service to the internet from a kubernetes pod.
