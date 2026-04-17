export default async function handler(req, res) {
  // CORS for WebGL builds
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') return res.status(200).end();
  if (req.method !== 'POST') return res.status(405).json({ error: 'Method not allowed' });

  const { code, code_verifier, redirect_uri, client_id } = req.body;

  if (!code || !code_verifier || !redirect_uri) {
    return res.status(400).json({ error: 'Missing required fields: code, code_verifier, redirect_uri' });
  }

  // Use client_id from request body, or fall back to env var
  const appClientId = client_id || process.env.DERIV_CLIENT_ID;
  if (!appClientId) {
    return res.status(500).json({ error: 'DERIV_CLIENT_ID not configured' });
  }

  try {
    const params = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: appClientId,
      code,
      code_verifier,
      redirect_uri,
    });

    const tokenResponse = await fetch('https://auth.deriv.com/oauth2/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params.toString(),
    });

    const data = await tokenResponse.json();

    if (!tokenResponse.ok) {
      console.error('[auth/token] Deriv token exchange failed:', data);
      return res.status(tokenResponse.status).json({ error: data.error_description || data.error || 'Token exchange failed' });
    }

    // Return only what Unity needs — don't expose refresh_token
    return res.status(200).json({
      access_token: data.access_token,
      expires_in: data.expires_in,
    });
  } catch (err) {
    console.error('[auth/token] Unexpected error:', err);
    return res.status(500).json({ error: 'Internal server error' });
  }
}
