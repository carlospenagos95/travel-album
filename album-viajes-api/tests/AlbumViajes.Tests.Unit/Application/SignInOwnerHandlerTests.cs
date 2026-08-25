using AlbumViajes.Application.Abstractions;
using AlbumViajes.Application.Auth.UseCases;
using AlbumViajes.Domain.Entities;
using NSubstitute;

namespace AlbumViajes.Tests.Unit.Application;

public sealed class SignInOwnerHandlerTests
{
    private const string Owner = "duenio@ejemplo.com";
    private const string RefreshToken = "token-de-google";

    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly IOwnerAllowlist _allowlist = Substitute.For<IOwnerAllowlist>();
    private readonly IGoogleAccountRepository _accounts = Substitute.For<IGoogleAccountRepository>();
    private readonly ISecretProtector _protector = Substitute.For<ISecretProtector>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SignInOwnerHandler _handler;

    public SignInOwnerHandlerTests()
    {
        _protector.Protect(Arg.Any<string>()).Returns(call => $"cifrado:{call.Arg<string>()}");
        _handler = new SignInOwnerHandler(_allowlist, _accounts, _protector, _unitOfWork, new FixedClock(Now));
    }

    [Fact]
    public async Task Un_correo_fuera_de_la_lista_no_entra()
    {
        _allowlist.Allows(Arg.Any<string>()).Returns(false);

        var result = await _handler.HandleAsync("intruso@ejemplo.com", RefreshToken, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("auth.notAllowed", result.Error!.Code);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<GoogleAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task La_primera_entrada_guarda_el_token_cifrado()
    {
        Allow();
        _accounts.FindByEmailAsync(Owner, Arg.Any<CancellationToken>()).Returns((GoogleAccount?)null);

        var result = await _handler.HandleAsync(Owner, RefreshToken, CancellationToken.None);

        Assert.True(result.IsSuccess);

        await _accounts.Received(1).AddAsync(
            Arg.Is<GoogleAccount>(account =>
                account.Email == Owner && account.ProtectedRefreshToken == $"cifrado:{RefreshToken}"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task El_correo_se_normaliza_antes_de_guardarlo()
    {
        Allow();
        _accounts.FindByEmailAsync(Owner, Arg.Any<CancellationToken>()).Returns((GoogleAccount?)null);

        var result = await _handler.HandleAsync("  DUENIO@Ejemplo.COM ", RefreshToken, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _accounts.Received(1).AddAsync(
            Arg.Is<GoogleAccount>(account => account.Email == Owner),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Entrar_de_nuevo_sin_token_conserva_el_permiso_ya_concedido()
    {
        Allow();
        var existing = GoogleAccount.Create(Owner, "cifrado:anterior", Now).Value;
        _accounts.FindByEmailAsync(Owner, Arg.Any<CancellationToken>()).Returns(existing);

        // Google solo emite el refresh token la primera vez que se concede el permiso.
        var result = await _handler.HandleAsync(Owner, refreshToken: null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cifrado:anterior", existing.ProtectedRefreshToken);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task La_primera_entrada_sin_token_avisa_de_que_falta_el_permiso()
    {
        Allow();
        _accounts.FindByEmailAsync(Owner, Arg.Any<CancellationToken>()).Returns((GoogleAccount?)null);

        var result = await _handler.HandleAsync(Owner, refreshToken: null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("auth.consentIncomplete", result.Error!.Code);
    }

    [Fact]
    public async Task Un_token_nuevo_reemplaza_al_guardado()
    {
        Allow();
        var existing = GoogleAccount.Create(Owner, "cifrado:anterior", Now).Value;
        _accounts.FindByEmailAsync(Owner, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.HandleAsync(Owner, "token-nuevo", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("cifrado:token-nuevo", existing.ProtectedRefreshToken);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void Allow() => _allowlist.Allows(Arg.Any<string>()).Returns(true);
}
