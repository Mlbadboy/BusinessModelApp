using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.WorldModel
{
    public interface ICompanyWorldModel
    {
        /// <summary>
        /// Captures an authoritative point-in-time snapshot of the company.
        /// Resolves field-by-field truth metrics linking to verified evidence records.
        /// Invariant: Distinguishes between Unavailable, VerifiedZero, Verified, and PartiallyVerified.
        /// </summary>
        Task<CompanySnapshot> CaptureVerifiedSnapshotAsync(
            Guid workspaceId,
            CancellationToken ct = default);
    }
}
