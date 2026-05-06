# Jira Ticket Tooltip — Setup Guide

## Prerequisites

You need an **Atlassian Developer Console** app with OAuth 2.0 configured.

## Step 1: Create OAuth 2.0 App

1. Go to: https://developer.atlassian.com/console/myapps/
2. Click **Create** → **OAuth 2.0 integration**
3. Name it (e.g. `VS Jira Ticket Tooltip`) → **Create**

## Step 2: Configure Authorization

1. Go to **Authorization** tab
2. Click **Add** next to `OAuth 2.0 (3LO)`
3. Set **Callback URL**: `http://localhost:9089/callback`
4. Click **Save changes**

## Step 3: Configure Permissions

1. Go to **Permissions** tab
2. Click **Add** next to `Jira API`
3. Enable scopes:
   - `read:jira-work`
   - `read:jira-user` (optional)
4. Click **Save**

## Step 4: Get Credentials

1. Go to **Settings** tab
2. Copy **Client ID** (looks like: `xYz123AbCdEfGhIjKlMnOpQrStUvWx`)
3. Click **Create new secret** → copy the secret (looks like: `ATOAxxxxxxxx...`)

## Step 5: Configure in Visual Studio

1. **Tools → Options → Jira Ticket Tooltip**:
   - Set **Jira Instance URL**: `https://yourcompany.atlassian.net`
   - Set **OAuth2 Client ID**: paste Client ID from step 4
   - Set **Enable Extension**: True

2. **Extensions → Configure Jira Connection...**:
   - Paste **Client Secret** from step 4
   - Click **Connect to Jira**
   - Browser opens → log in to Atlassian → authorize the app
   - Status changes to ✅ Connected

## Step 6: Use

Open any code file with a comment containing a Jira ticket ID (e.g. `// ABC-123`).
A CodeLens will appear above the line showing the ticket title.
Click the CodeLens to open the ticket in your browser.

## Troubleshooting

| Error | Solution |
|-------|----------|
| "Nie można zidentyfikować aplikacji" | Client ID is wrong, or Authorization callback URL is not set |
| "Authorization failed — no code received" | Callback URL mismatch — must be exactly `http://localhost:9089/callback` |
| "State mismatch" | Try again — possible browser cache issue |
| "Token exchange failed" | Client Secret is wrong |
| "No access token available" | Click Connect again — tokens may have expired |
