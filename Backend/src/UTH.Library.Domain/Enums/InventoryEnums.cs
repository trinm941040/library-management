namespace UTH.Library.Domain.Enums;
public enum CopyCondition { New, Good, Worn, Damaged, Lost }
public enum CopyStatus { Available, Borrowed, Reserved, InTransit, Lost, Damaged, Withdrawn }
public enum ShelfStatus { Active, Inactive }
public enum InventoryAuditStatus { Draft, InProgress, Completed, Cancelled }
public enum AuditItemResult { Pending, Found, Misplaced, Missing, Damaged }
