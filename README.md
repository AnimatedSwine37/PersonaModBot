# PersonaModBot
A discord bot for miscellaneous moderating needs on the [Persona Modding discord server](https://discord.gg/naoto).

## Setup
Whilst this is primarily designed for use on the Persona Modding server it should be usable on other ones. 

This bot has a [Docker container](ghcr.io/animatedswine37/personamodbot) that you can use to (relatively easily) setup and run it.

The bot makes use of [EdgeDB](https://www.edgedb.com/) for its database so you will first need to setup an edgedb instance using the schema in the [dbschema](dbschema) folder. Once you have a running EdgeDB instance you can then use the bot by setting the `EDGEDB_DSN` environment variable to a valid [dsn](https://www.edgedb.com/docs/reference/dsn) for your instance and the `BOT_TOKEN` environment variable to the token for your discord bot.

Optionally you can also set the `EDGEDB_LOG_LEVEL` and `BOT_LOG_LEVEL` to one of the following: `CRITICAL`, `ERROR`, `WARNING`, `INFO`, `DEBUG`, `TRACE`. If no level is set for either then it will default to `INFO`.
