namespace AppController.Controllers;

public class CookieHelper
{
    public static void SetHttpOnlyCookie(string key, string value, int expireMinutes, HttpResponse response)
    {
        response.Cookies.Append(key, value, new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTimeOffset.Now.AddMinutes(expireMinutes),
            SameSite = SameSiteMode.None
        });
    }
}