using BusinessModelApp.Core.Domain.BusinessModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface IBusinessModelService
    {
        // Business Model operations
        Task<BusinessModel> GetBusinessModelByIdAsync(Guid id, bool includeRelated = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<BusinessModel>> GetBusinessModelsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<BusinessModel> CreateBusinessModelAsync(BusinessModel model, Guid createdById, CancellationToken cancellationToken = default);
        Task<BusinessModel> UpdateBusinessModelAsync(BusinessModel model, CancellationToken cancellationToken = default);
        Task<bool> DeleteBusinessModelAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        Task<bool> PublishBusinessModelAsync(Guid id, Guid publishedById, CancellationToken cancellationToken = default);
        Task<bool> RetractBusinessModelAsync(Guid id, Guid retractedById, CancellationToken cancellationToken = default);
        
        // Revenue Stream operations
        Task<RevenueStream> AddRevenueStreamAsync(RevenueStream revenueStream, CancellationToken cancellationToken = default);
        Task<RevenueStream> UpdateRevenueStreamAsync(RevenueStream revenueStream, CancellationToken cancellationToken = default);
        Task<bool> RemoveRevenueStreamAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Cost Structure operations
        Task<CostStructure> AddCostStructureAsync(CostStructure costStructure, CancellationToken cancellationToken = default);
        Task<CostStructure> UpdateCostStructureAsync(CostStructure costStructure, CancellationToken cancellationToken = default);
        Task<bool> RemoveCostStructureAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Key Activity operations
        Task<KeyActivity> AddKeyActivityAsync(KeyActivity keyActivity, CancellationToken cancellationToken = default);
        Task<KeyActivity> UpdateKeyActivityAsync(KeyActivity keyActivity, CancellationToken cancellationToken = default);
        Task<bool> RemoveKeyActivityAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Key Resource operations
        Task<KeyResource> AddKeyResourceAsync(KeyResource keyResource, CancellationToken cancellationToken = default);
        Task<KeyResource> UpdateKeyResourceAsync(KeyResource keyResource, CancellationToken cancellationToken = default);
        Task<bool> RemoveKeyResourceAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Key Partnership operations
        Task<KeyPartnership> AddKeyPartnershipAsync(KeyPartnership keyPartnership, CancellationToken cancellationToken = default);
        Task<KeyPartnership> UpdateKeyPartnershipAsync(KeyPartnership keyPartnership, CancellationToken cancellationToken = default);
        Task<bool> RemoveKeyPartnershipAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Customer Segment operations
        Task<CustomerSegment> AddCustomerSegmentAsync(CustomerSegment customerSegment, CancellationToken cancellationToken = default);
        Task<CustomerSegment> UpdateCustomerSegmentAsync(CustomerSegment customerSegment, CancellationToken cancellationToken = default);
        Task<bool> RemoveCustomerSegmentAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Channel operations
        Task<Channel> AddChannelAsync(Channel channel, CancellationToken cancellationToken = default);
        Task<Channel> UpdateChannelAsync(Channel channel, CancellationToken cancellationToken = default);
        Task<bool> RemoveChannelAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Customer Relationship operations
        Task<CustomerRelationship> AddCustomerRelationshipAsync(CustomerRelationship customerRelationship, CancellationToken cancellationToken = default);
        Task<CustomerRelationship> UpdateCustomerRelationshipAsync(CustomerRelationship customerRelationship, CancellationToken cancellationToken = default);
        Task<bool> RemoveCustomerRelationshipAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Value Proposition operations
        Task<ValueProposition> AddValuePropositionAsync(ValueProposition valueProposition, CancellationToken cancellationToken = default);
        Task<ValueProposition> UpdateValuePropositionAsync(ValueProposition valueProposition, CancellationToken cancellationToken = default);
        Task<bool> RemoveValuePropositionAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
        
        // Relationship management
        Task<bool> LinkValuePropositionToRevenueStreamAsync(Guid valuePropositionId, Guid revenueStreamId, CancellationToken cancellationToken = default);
        Task<bool> UnlinkValuePropositionFromRevenueStreamAsync(Guid valuePropositionId, Guid revenueStreamId, CancellationToken cancellationToken = default);
        Task<bool> LinkValuePropositionToCustomerSegmentAsync(Guid valuePropositionId, Guid customerSegmentId, CancellationToken cancellationToken = default);
        Task<bool> UnlinkValuePropositionFromCustomerSegmentAsync(Guid valuePropositionId, Guid customerSegmentId, CancellationToken cancellationToken = default);
        Task<bool> LinkKeyResourceToActivityAsync(Guid resourceId, Guid activityId, CancellationToken cancellationToken = default);
        Task<bool> UnlinkKeyResourceFromActivityAsync(Guid resourceId, Guid activityId, CancellationToken cancellationToken = default);
    }
}
