namespace UTH.Library.Domain.Entities;

public sealed class CirculationPolicy
{
    public const int DefaultMaxLoanBooks = 5;
    public const int DefaultLoanPeriodDays = 14;
    public const int DefaultMaxRenewals = 2;
    public const int DefaultRenewalPeriodDays = 7;
    public const int DefaultHoldDays = 3;
    public const decimal DefaultFinePerDay = 5000m;
    public const decimal DefaultMaxFineAmount = 100000m;
    public const decimal DefaultLostBookPenaltyRatio = 150m;

    private CirculationPolicy()
    {
        Name = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }

    // Scope (Phạm vi áp dụng: null hoặc trống nghĩa là áp dụng toàn bộ/All)
    public string? MemberGroup { get; private set; }
    public string? DocumentType { get; private set; }
    public Guid? BranchId { get; private set; }

    // Khoảng thời gian hiệu lực (Effective Range)
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    // BorrowingLimit (Giới hạn mượn)
    public int MaxLoanBooks { get; private set; }
    public int LoanPeriodDays { get; private set; }
    public int MaxRenewals { get; private set; }
    public int RenewalPeriodDays { get; private set; }
    public int HoldDays { get; private set; }
    public bool BlockIfOverdue { get; private set; }

    // FinePolicy (Quy định phạt - decimal precision)
    public decimal FinePerDay { get; private set; }
    public decimal FixedFineAmount { get; private set; }
    public decimal MaxFineAmount { get; private set; }
    public decimal LostBookPenaltyRatio { get; private set; }

    // Audit tracking
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public static CirculationPolicy Create(
        string name,
        string? description,
        string? memberGroup,
        string? documentType,
        Guid? branchId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        int maxLoanBooks,
        int loanPeriodDays,
        int maxRenewals,
        int renewalPeriodDays,
        int holdDays,
        bool blockIfOverdue,
        decimal finePerDay,
        decimal fixedFineAmount,
        decimal maxFineAmount,
        decimal lostBookPenaltyRatio,
        bool isActive,
        Guid? createdByUserId,
        DateTime now,
        int version = 1)
    {
        Validate(name, effectiveFrom, effectiveTo, maxLoanBooks, loanPeriodDays, maxRenewals, renewalPeriodDays, holdDays, finePerDay, fixedFineAmount, maxFineAmount, lostBookPenaltyRatio);

        return new CirculationPolicy
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Version = Math.Max(1, version),
            IsActive = isActive,
            MemberGroup = NormalizeScope(memberGroup),
            DocumentType = NormalizeScope(documentType),
            BranchId = branchId == Guid.Empty ? null : branchId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            MaxLoanBooks = maxLoanBooks,
            LoanPeriodDays = loanPeriodDays,
            MaxRenewals = maxRenewals,
            RenewalPeriodDays = renewalPeriodDays,
            HoldDays = holdDays,
            BlockIfOverdue = blockIfOverdue,
            FinePerDay = finePerDay,
            FixedFineAmount = fixedFineAmount,
            MaxFineAmount = maxFineAmount,
            LostBookPenaltyRatio = lostBookPenaltyRatio,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = null
        };
    }

    public void Update(
        string name,
        string? description,
        string? memberGroup,
        string? documentType,
        Guid? branchId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        int maxLoanBooks,
        int loanPeriodDays,
        int maxRenewals,
        int renewalPeriodDays,
        int holdDays,
        bool blockIfOverdue,
        decimal finePerDay,
        decimal fixedFineAmount,
        decimal maxFineAmount,
        decimal lostBookPenaltyRatio,
        DateTime now)
    {
        Validate(name, effectiveFrom, effectiveTo, maxLoanBooks, loanPeriodDays, maxRenewals, renewalPeriodDays, holdDays, finePerDay, fixedFineAmount, maxFineAmount, lostBookPenaltyRatio);

        Name = name.Trim();
        Description = description?.Trim();
        MemberGroup = NormalizeScope(memberGroup);
        DocumentType = NormalizeScope(documentType);
        BranchId = branchId == Guid.Empty ? null : branchId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        MaxLoanBooks = maxLoanBooks;
        LoanPeriodDays = loanPeriodDays;
        MaxRenewals = maxRenewals;
        RenewalPeriodDays = renewalPeriodDays;
        HoldDays = holdDays;
        BlockIfOverdue = blockIfOverdue;
        FinePerDay = finePerDay;
        FixedFineAmount = fixedFineAmount;
        MaxFineAmount = maxFineAmount;
        LostBookPenaltyRatio = lostBookPenaltyRatio;
        UpdatedAtUtc = now;
    }

    public CirculationPolicy CreateNextVersion(
        string? name,
        string? description,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        int? maxLoanBooks,
        int? loanPeriodDays,
        int? maxRenewals,
        int? renewalPeriodDays,
        int? holdDays,
        bool? blockIfOverdue,
        decimal? finePerDay,
        decimal? fixedFineAmount,
        decimal? maxFineAmount,
        decimal? lostBookPenaltyRatio,
        Guid? createdByUserId,
        DateTime now)
    {
        return Create(
            name ?? Name,
            description ?? Description,
            MemberGroup,
            DocumentType,
            BranchId,
            effectiveFrom,
            effectiveTo,
            maxLoanBooks ?? MaxLoanBooks,
            loanPeriodDays ?? LoanPeriodDays,
            maxRenewals ?? MaxRenewals,
            renewalPeriodDays ?? RenewalPeriodDays,
            holdDays ?? HoldDays,
            blockIfOverdue ?? BlockIfOverdue,
            finePerDay ?? FinePerDay,
            fixedFineAmount ?? FixedFineAmount,
            maxFineAmount ?? MaxFineAmount,
            lostBookPenaltyRatio ?? LostBookPenaltyRatio,
            isActive: false, // version mới ban đầu ở trạng thái draft/inactive
            createdByUserId,
            now,
            version: Version + 1);
    }

    public void Activate(DateTime now)
    {
        IsActive = true;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    public bool OverlapsWith(CirculationPolicy other)
    {
        if (Id == other.Id) return false;

        // Cùng phạm vi
        bool sameMember = string.Equals(MemberGroup, other.MemberGroup, StringComparison.OrdinalIgnoreCase);
        bool sameDoc = string.Equals(DocumentType, other.DocumentType, StringComparison.OrdinalIgnoreCase);
        bool sameBranch = BranchId == other.BranchId;

        if (!sameMember || !sameDoc || !sameBranch)
            return false;

        // Kiểm tra khoảng thời gian giao nhau: [StartA, EndA] và [StartB, EndB]
        var startA = EffectiveFrom;
        var endA = EffectiveTo ?? DateTime.MaxValue;
        var startB = other.EffectiveFrom;
        var endB = other.EffectiveTo ?? DateTime.MaxValue;

        return startA < endB && startB < endA;
    }

    private static string? NormalizeScope(string? val)
    {
        if (string.IsNullOrWhiteSpace(val) || val.Trim().Equals("All", StringComparison.OrdinalIgnoreCase))
            return null;
        return val.Trim();
    }

    private static void Validate(
        string name,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        int maxLoanBooks,
        int loanPeriodDays,
        int maxRenewals,
        int renewalPeriodDays,
        int holdDays,
        decimal finePerDay,
        decimal fixedFineAmount,
        decimal maxFineAmount,
        decimal lostBookPenaltyRatio)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Policy name is required.", nameof(name));
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("EffectiveTo must be after EffectiveFrom.", nameof(effectiveTo));
        if (maxLoanBooks < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLoanBooks), "Max loan books must be at least 1.");
        if (loanPeriodDays < 1)
            throw new ArgumentOutOfRangeException(nameof(loanPeriodDays), "Loan period days must be at least 1.");
        if (maxRenewals < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRenewals), "Max renewals cannot be negative.");
        if (renewalPeriodDays < 1)
            throw new ArgumentOutOfRangeException(nameof(renewalPeriodDays), "Renewal period days must be at least 1.");
        if (holdDays < 1)
            throw new ArgumentOutOfRangeException(nameof(holdDays), "Hold days must be at least 1.");
        if (finePerDay < 0)
            throw new ArgumentOutOfRangeException(nameof(finePerDay), "Fine per day cannot be negative.");
        if (fixedFineAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(fixedFineAmount), "Fixed fine amount cannot be negative.");
        if (maxFineAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxFineAmount), "Max fine amount cannot be negative.");
        if (maxFineAmount > 0 && maxFineAmount < fixedFineAmount)
            throw new ArgumentException("Max fine amount cannot be less than fixed fine amount.", nameof(maxFineAmount));
        if (lostBookPenaltyRatio < 0)
            throw new ArgumentOutOfRangeException(nameof(lostBookPenaltyRatio), "Lost book penalty ratio cannot be negative.");
    }
}
