using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Kadrovska.Api.Infrastruktura;

/// <summary>
/// Pristup dijeljenoj in-memory SQLite bazi. Baza postoji dok je otvorena barem jedna veza, pa se
/// jedna veza drži otvorenom cijelo vrijeme rada aplikacije. Svaki zahtjev otvara vlastitu vezu.
/// </summary>
public sealed class Baza : IDisposable
{
    private readonly SqliteConnection _trajnaVeza;

    public Baza(PostavkeAplikacije postavke)
    {
        ConnectionString = postavke.ConnectionString;
        _trajnaVeza = new SqliteConnection(ConnectionString);
        _trajnaVeza.Open();
    }

    public string ConnectionString { get; }

    /// <summary>Otvara novu vezu prema bazi (pozivatelj je zatvara).</summary>
    public SqliteConnection Otvori()
    {
        var veza = new SqliteConnection(ConnectionString);
        veza.Open();
        return veza;
    }

    /// <summary>Parametrizirani SELECT – retci kao rječnici (naziv stupca → vrijednost).</summary>
    public List<Dictionary<string, object?>> Upit(string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var veza = Otvori();
        return Upit(veza, null, sql, parametri);
    }

    /// <summary>Parametrizirani INSERT/UPDATE/DELETE – vraća broj promijenjenih redaka.</summary>
    public int Izvrsi(string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var veza = Otvori();
        return Izvrsi(veza, null, sql, parametri);
    }

    /// <summary>Parametrizirani upit koji vraća jednu vrijednost.</summary>
    public object? Skalar(string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var veza = Otvori();
        return Skalar(veza, null, sql, parametri);
    }

    /// <summary>INSERT koji vraća ID novog retka.</summary>
    public long Umetni(string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var veza = Otvori();
        return Umetni(veza, null, sql, parametri);
    }

    public static List<Dictionary<string, object?>> Upit(
        SqliteConnection veza, SqliteTransaction? transakcija, string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var naredba = Naredba(veza, transakcija, sql, parametri);
        using var citac = naredba.ExecuteReader();
        var redci = new List<Dictionary<string, object?>>();
        while (citac.Read())
        {
            var redak = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < citac.FieldCount; i++)
            {
                redak[citac.GetName(i)] = citac.IsDBNull(i) ? null : citac.GetValue(i);
            }
            redci.Add(redak);
        }
        return redci;
    }

    public static int Izvrsi(
        SqliteConnection veza, SqliteTransaction? transakcija, string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var naredba = Naredba(veza, transakcija, sql, parametri);
        return naredba.ExecuteNonQuery();
    }

    public static object? Skalar(
        SqliteConnection veza, SqliteTransaction? transakcija, string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        using var naredba = Naredba(veza, transakcija, sql, parametri);
        var rezultat = naredba.ExecuteScalar();
        return rezultat is DBNull ? null : rezultat;
    }

    public static long Umetni(
        SqliteConnection veza, SqliteTransaction? transakcija, string sql, params (string Naziv, object? Vrijednost)[] parametri)
    {
        Izvrsi(veza, transakcija, sql, parametri);
        return Convert.ToInt64(Skalar(veza, transakcija, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    private static SqliteCommand Naredba(
        SqliteConnection veza, SqliteTransaction? transakcija, string sql, (string Naziv, object? Vrijednost)[] parametri)
    {
        var naredba = veza.CreateCommand();
        naredba.CommandText = sql;
        naredba.Transaction = transakcija;
        foreach (var (naziv, vrijednost) in parametri)
        {
            naredba.Parameters.AddWithValue(naziv, vrijednost ?? DBNull.Value);
        }
        return naredba;
    }

    public void Dispose() => _trajnaVeza.Dispose();
}

/// <summary>Čitanje vrijednosti iz retka koji vraća <c>Baza.Upit</c>.</summary>
public static class RedakExtensions
{
    public static string Tekst(this Dictionary<string, object?> redak, string stupac) =>
        Convert.ToString(redak[stupac], CultureInfo.InvariantCulture) ?? string.Empty;

    public static long Broj(this Dictionary<string, object?> redak, string stupac) =>
        Convert.ToInt64(redak[stupac], CultureInfo.InvariantCulture);

    public static decimal Iznos(this Dictionary<string, object?> redak, string stupac) =>
        Convert.ToDecimal(redak[stupac], CultureInfo.InvariantCulture);
}
