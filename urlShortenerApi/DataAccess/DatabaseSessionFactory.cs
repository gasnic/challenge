using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Cassandra;
using Microsoft.Extensions.Options;

public class DatabaseSessionFactory
{
    private DatabaseConnectionConfigurations _databaseOptions;
    public DatabaseSessionFactory(IOptions<DatabaseConnectionConfigurations> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public virtual ISession Create()
    {
        try
        {
            var options = new Cassandra.SSLOptions(SslProtocols.Tls12, true, ValidateServerCertificate);
            options.SetHostNameResolver((ipAddress) => _databaseOptions.CassandraContactPoint);
            Cluster cluster = Cluster.Builder()
                .WithCredentials(_databaseOptions.UserName, _databaseOptions.Password)
                .WithPort(_databaseOptions.CassandraPort)
                .AddContactPoint(_databaseOptions.CassandraContactPoint)
                .WithSSL(options)
                .Build();

            return cluster.Connect("urlshortener");
        }
        catch (Exception e)
        {
            throw new Exception("Error connecting to database", e);
        }
    }

    public static bool ValidateServerCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;
        return false;
    }
}
