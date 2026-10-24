// Jakar.Extensions :: Jakar.Database
// 05/22/2023  11:38 AM

namespace Jakar.Database;


public static class WebAppExtensions
{
    public static WebApplication UseUrls( this WebApplication app, params ReadOnlySpan<string> urls )
    {
        foreach ( string url in urls ) { app.Urls.Add(url); }

        return app;
    }



    /// <param name="self"> The <see cref="WebApplicationBuilder"/> to configure. </param>
    extension( WebApplicationBuilder self )
    {
        /// <summary> Specify the urls the web host will listen on. </summary>
        /// <param name="urls"> The urls the hosted application will listen on. </param>
        /// <returns> The <see cref="WebApplicationBuilder"/> . </returns>
        public void UseUrls( params string[] urls )
        {
            if ( urls is null ) { throw new ArgumentNullException(nameof(urls)); }

            self.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(';', urls));
        }
        /// <summary> Specify the webroot directory to be used by the web host. </summary>
        /// <param name="webRoot"> Path to the root directory used by the web server. </param>
        /// <returns> The <see cref="WebApplicationBuilder"/> . </returns>
        public void UseWebRoot( string webRoot )
        {
            if ( webRoot is null ) { throw new ArgumentNullException(nameof(webRoot)); }

            self.WebHost.UseSetting(WebHostDefaults.WebRootKey, webRoot);
        }
    }
}
