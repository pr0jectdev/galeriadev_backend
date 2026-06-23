using ImageGallery.Api.Common;

namespace ImageGallery.Api.Services;

/// <summary>
/// Mantém uma única instância do cliente Supabase, autenticado com a
/// SERVICE ROLE KEY. Como o backend já controla toda a autorização
/// (via JWT + tabela profiles), o cliente aqui ignora o RLS por design —
/// o RLS no banco fica como camada extra de proteção.
///
/// O cliente em si é criado e inicializado (await InitializeAsync()) uma
/// única vez, em Program.cs, antes da aplicação subir — evitando qualquer
/// chamada bloqueante (.Result / .GetAwaiter().GetResult()) em runtime.
/// </summary>
public interface ISupabaseClientProvider
{
    Supabase.Client Client { get; }
}

public class SupabaseClientProvider(Supabase.Client client) : ISupabaseClientProvider
{
    public Supabase.Client Client { get; } = client;
}

