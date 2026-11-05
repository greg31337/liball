using cookies;

var parser = new CookieParser();
var serializer = new CookieSerializer();

// Response cookies include attributes such as expiry, path, and browser restrictions.
var cookie = parser.ParseSetCookie(
    "theme=dark; Path=/; Max-Age=3600; Secure; HttpOnly; SameSite=Lax",
    new CookieParseOptions());
Console.WriteLine($"Cookie: {cookie.Name}={cookie.Value}");
Console.WriteLine($"Path: {cookie.Path}; lifetime: {cookie.MaxAgeSeconds} seconds");
Console.WriteLine($"Secure={cookie.Secure}, HttpOnly={cookie.HttpOnly}, SameSite={cookie.SameSiteMode}");
string header = serializer.SerializeSetCookie(cookie, new CookieSerializationOptions());
Console.WriteLine($"Set-Cookie: {header}");

// Request cookies carry only name/value pairs back to the server.
var pairs = parser.ParseCookieFields(new[] { "theme=dark; language=en" }, new CookieParseOptions());
Console.WriteLine($"Cookie: {serializer.SerializeCookiePairs(pairs, new CookieSerializationOptions())}");
Console.WriteLine($"Request contains {pairs.Length} name/value pairs; attributes belong to Set-Cookie.");
