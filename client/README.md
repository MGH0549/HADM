# Hadm client

This is the standalone Angular frontend for Hadm.

## Run

Start the API from the repository root with `dotnet run --project src/Hadm.WebApi`, then run `npm start` from this directory. The Angular development server proxies `/api` to `http://localhost:5292`. Open `http://localhost:4200`.

## Build

Run `npm run build` to create the production bundle in `dist/client`.

The app supports English and Arabic, including RTL layout. Authentication uses a JWT interceptor; the API must be configured with the matching development or deployment origin.
