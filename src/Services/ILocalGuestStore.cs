using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface ILocalGuestStore
{
	Task<GuestDataSnapshot> LoadAsync(CancellationToken cancellationToken = default);

	Task SaveAsync(GuestDataSnapshot snapshot, CancellationToken cancellationToken = default);

	Task ClearAsync(CancellationToken cancellationToken = default);
}
