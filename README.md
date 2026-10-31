# Butler Jaimes

**J**ust-in-**T**ime **AI** **M**anagement **S**ystem: a DIY AI butler that lives in your hallway mirror.

Jaimes shows your weekly plan, shopping list and to-dos on a Magic Mirror, controls your home by voice, and helps plan the household and optimize your day.

## Features
- Voice control with local wake word and speech-to-text, answered by an LLM with a butler personality
- Smart home control through Home Assistant (lighting, plugs, shutters), using Eltako actuators on an RS485 bus
- Mirror UI built with Blazor Server (.NET 8), with live updates over MQTT
- Meal Week integration: weekly meal plan, recipes and shopping list
- Morning energy briefing based on Fronius PV data and the weather forecast
- Calendar, reminders and AI-generated routines
- Local-first and resilient: graceful fallbacks if the server or an integration is down

## Architecture
- **Raspberry Pi:** audio I/O, wake word, LED status ring, Chromium kiosk display
- **Home server:** AI processing, Home Assistant, Mosquitto, Blazor Server, SQLite

## Tech stack
Python (FastAPI, Whisper, Porcupine, Piper) · .NET 8 / Blazor Server · Home Assistant · MQTT · SQLite · Claude API

## Status
Work in progress. The hardware is being installed in a house under construction.

## License
See [LICENSE](LICENSE).
