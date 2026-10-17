using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Players.Whitelist;

/// <summary>
/// Managed for caching whitelist statuses for connected players on the server.
/// If trying to get the data of disconnected players, use <see cref="IServerDbManager.GetWhitelistStatusAsync"/> directly.
/// </summary>
// TODO: This manager can likely be converted into a more general purpose manager once we expand the IServerDbManager's functions.
public sealed partial class WhitelistManager : IPostInjectInit
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private ILogManager _logManager = default!;

    private readonly ConcurrentDictionary<NetUserId, bool> _whitelistStatus = new();
    // Host promotion and connection admission may request the same whitelist write concurrently.
    // Serialize status reads and mutations so check-then-insert cannot race inside this server.
    private readonly SemaphoreSlim _whitelistStatusLock = new(1, 1);
    private ISawmill _sawmill = default!;

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        await _whitelistStatusLock.WaitAsync(cancel);
        try
        {
            var whitelistStatus = await _db.GetWhitelistStatusAsync(session.UserId);
            _whitelistStatus[session.UserId] = whitelistStatus;
        }
        finally
        {
            _whitelistStatusLock.Release();
        }
    }

    private void ClientDisconnected(ICommonSession session)
    {
        _whitelistStatus.TryRemove(session.UserId, out _);
    }

    /// <inheritdoc cref="IsConnectedWhitelisted(Robust.Shared.Network.NetUserId)"/>
    public bool IsConnectedWhitelisted(ICommonSession session)
    {
        return IsConnectedWhitelisted(session.UserId);
    }

    /// <summary>
    /// Returns true if the player has whitelist status on the server, false if they don't.
    /// Returns false and raises an error if the player is not in the cached database, meaning they are either not connected or database failed.
    /// </summary>
    public bool IsConnectedWhitelisted(NetUserId userId)
    {
        if (!_whitelistStatus.TryGetValue(userId, out var whitelistStatus))
        {
            _sawmill.Error("{Player} is either not connected, or the database failed to load their whitelist status. Stack trace:\\n{StackTrace}",
                userId,
                Environment.StackTrace);
            return false;
        }

        return whitelistStatus;
    }

    /// <summary>
    /// Adds a player to the whitelist and updates the tracker.
    /// </summary>
    public async void AddWhitelist(NetUserId player)
    {
        await AddWhitelistAsync(player);
    }

    /// <summary>
    /// Adds a player to the persisted whitelist and updates the cache after the database write completes.
    /// Await this from connection checks so the allow decision cannot race the whitelist write.
    /// </summary>
    public async Task AddWhitelistAsync(NetUserId player)
    {
        await _whitelistStatusLock.WaitAsync();
        try
        {
            // Host promotion and connection admission can request this concurrently.
            // The lock covers both the database existence check and insert.
            if (!await _db.GetWhitelistStatusAsync(player))
                await _db.AddToWhitelistAsync(player);

            if (_whitelistStatus.ContainsKey(player))
                _whitelistStatus[player] = true;
        }
        finally
        {
            _whitelistStatusLock.Release();
        }
    }

    /// <summary>
    /// Removes a player from the whitelist and updates the tracker.
    /// </summary>
    public async void RemoveWhitelist(NetUserId player)
    {
        await _whitelistStatusLock.WaitAsync();
        try
        {
            await _db.RemoveFromWhitelistAsync(player);

            if (_whitelistStatus.ContainsKey(player))
                _whitelistStatus[player] = false;
        }
        finally
        {
            _whitelistStatusLock.Release();
        }
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
        _sawmill = _logManager.GetSawmill("server_whitelist");
    }
}
