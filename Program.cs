namespace MedServices.Startup
{
	public class Program
	{
		public static void Main(string[] args)
		{
			try
			{
				CreateHostBuilder(args)?.Build()?.Run();
			}
			catch(Exception e)
			{
				Debug.WriteLine(e);
				throw;
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			IHostBuilder? builder = null;
			try
			{
				builder = Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
					{
						webBuilder.UseSetting(HostDefaults.ApplicationKey, "MedServices")
							.UseStartup<Startup>();
					});
			}
			catch(Exception e)
			{
				builder = null;
				Debug.WriteLine(e);
				throw;
			}
			return builder;
		}
    }
}
