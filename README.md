# Telegram AI Mediator

A self-bot built with WTelegramClient that reads both sides of a private Telegram chat and acts as a neutral third-party mediator: offering concise practical advice, one clear next step, and contextual light humor or lines from Persian poetry when appropriate. Routes traffic through MTProto proxies with rotation and health tracking and prioritizes safety by flagging urgent risk and recommending professionals for specialized advice.

---

## Key features

- Third-party mediator that reads both participants in a two-person Telegram chat.  
- Generates concise replies (typically 1–6 sentences) for each side, focused on one actionable next step.  
- Contextual humor and optional insertion of Persian poems or proverbs when appropriate.  
- MTProto proxy support with rotation, health tracking, and fallback strategies.  
- Configurable prompt file and proxy list file for runtime updates.  
- Session file path customizable via WTelegramClient Config callback.  
- Safety-first: recommends professionals for medical/legal/financial issues and flags urgent risk scenarios.

---

## Requirements

- .NET 6.0 or later  
- WTelegramClient NuGet package  
- A text-generation backend or LLM client (configurable)  
- Optional: MTProto proxy list for routing

---

## Quick start

1. Clone the repository and open it in your IDE.  
2. Add NuGet packages: WTelegramClient and your chosen LLM client.  
3. Create configuration (app settings or environment variables) with your Telegram credentials: `api_id`, `api_hash`.  
4. Provide two files in the app folder:  
   - `proxies.txt` — one MTProto proxy per line (e.g., `host:port:secret`).  
   - `prompt.txt` — the mediator system prompt.  
5. Start the app and follow WTelegramClient login flow (phone number + code). The session file will be saved to the path returned by your Config callback.

---

## Configuration examples

Config callback sample (session file path):
```csharp
static string Config(string what) => what switch
{
    "api_id" => "YOUR_API_ID",
    "api_hash" => "YOUR_API_HASH",
    "session_pathname" => "sessions/mediator.session",
    _ => null
};
```

Load proxies and pick a random MTProto proxy:
```csharp
var proxies = File.Exists("proxies.txt")
    ? File.ReadAllLines("proxies.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
    : new List<string>();

client.MTProxyUrl = proxies.Count > 0 ? proxies[Random.Shared.Next(proxies.Count)] : null;
```

---

## Recommended proxy strategies

- Probe and rank proxies in the background by success rate and latency.  
- Prefer parallel short probes for top candidates to reduce connection latency.  
- Persist health metrics to disk so restarts don’t re-test everything.  
- Fallback order: healthy MTProto → other MTProto → SOCKS5 (optional) → direct connection.  
- Use timeouts and CancellationToken to avoid long blocking attempts.

---

## Prompt and behavior guidance

- Keep the system prompt concise and strict about reply length and style (1–6 sentences).  
- Always include one practical next step for emotional or conflict situations.  
- Use jokes or Persian poetry sparingly and only when context fits.  
- Avoid inventing facts; respond with “I don’t know — need to check” when uncertain.  
- When acting as SELF for messages from “Jalal”/“Jaleh”/“Jalal Jaleh”, use first-person.

---


