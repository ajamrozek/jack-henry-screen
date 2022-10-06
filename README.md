# jack-henry-screen
Leveraging a `BackgroundService` with a `Channel` queueing governer to call the Twitter Sample Stream API with a Polly resilient HttpClient within a singular application runtime. 

# Api Token
To apply your API Token, apply it to the appsettings.json or secrets.json

`
"Twitter": {
    "ApiToken": "[insert token here]"
  }
`