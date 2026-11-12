using Microsoft.EntityFrameworkCore;

namespace Muflone.Persistence.Sql.Persistence;

/// <summary>
/// Idempotent bootstrapper for the EventStore schema. Creates the tables only when they are
/// missing, so it is safe to run on every startup and against databases where the schema was
/// already provisioned by hand or by a deployment pipeline.
/// </summary>
/// <remarks>
/// The <c>EventStore</c> table is a SQL Server append-only ledger table
/// (<c>LEDGER = ON (APPEND_ONLY = ON)</c>). The DDL below mirrors the schema documented in the
/// package README; keep the two in sync. The <c>GO</c> / <c>SET</c> batch separators from the
/// README are intentionally omitted because <see cref="RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync"/>
/// executes a single T-SQL batch and does not understand <c>GO</c>.
/// </remarks>
internal static class EventStoreSchema
{
	private const string CreateEventStoreTableSql = """
		IF NOT EXISTS (SELECT 1 FROM sys.tables t
			INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
			WHERE s.name = N'dbo' AND t.name = N'EventStore')
		BEGIN
			CREATE TABLE [dbo].[EventStore](
				[MessageId] [nvarchar](36) NOT NULL,
				[AggregateId] [nvarchar](36) NOT NULL,
				[AggregateName] [nvarchar](250) NOT NULL,
				[AggregateType] [nvarchar](250) NOT NULL,
				[EventType] [nvarchar](250) NOT NULL,
				[Data] [varbinary](max) NOT NULL,
				[Metadata] [varbinary](max) NOT NULL,
				[Version] [int] NOT NULL,
				[CommitPosition] [bigint] IDENTITY(1,1) NOT NULL,
				[ledger_start_transaction_id] [bigint] GENERATED ALWAYS AS transaction_id START HIDDEN NOT NULL,
				[ledger_start_sequence_number] [bigint] GENERATED ALWAYS AS sequence_number START HIDDEN NOT NULL,
			 CONSTRAINT [PK_EventStore] PRIMARY KEY CLUSTERED
			(
				[MessageId] ASC
			)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
			) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
			WITH
			(
			LEDGER = ON (APPEND_ONLY = ON, LEDGER_VIEW = [dbo].[EventStore_Ledger] (TRANSACTION_ID_COLUMN_NAME = [ledger_transaction_id], SEQUENCE_NUMBER_COLUMN_NAME = [ledger_sequence_number], OPERATION_TYPE_COLUMN_NAME = [ledger_operation_type], OPERATION_TYPE_DESC_COLUMN_NAME = [ledger_operation_type_desc]))
			);
		END
		""";

	private const string CreateEventStorePositionTableSql = """
		IF NOT EXISTS (SELECT 1 FROM sys.tables t
			INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
			WHERE s.name = N'dbo' AND t.name = N'EventStorePosition')
		BEGIN
			CREATE TABLE [dbo].[EventStorePosition](
				[Id] [int] IDENTITY(1,1) NOT NULL,
				[CommitPosition] [bigint] NOT NULL,
				[PreparePosition] [bigint] NOT NULL,
			 CONSTRAINT [PK_EventStorePosition_1] PRIMARY KEY CLUSTERED
			(
				[Id] ASC
			)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
			) ON [PRIMARY];
		END
		""";

	public static async Task EnsureCreatedAsync(EventStoreContext context, CancellationToken cancellationToken = default)
	{
		await context.Database.ExecuteSqlRawAsync(CreateEventStoreTableSql, cancellationToken);
		await context.Database.ExecuteSqlRawAsync(CreateEventStorePositionTableSql, cancellationToken);
	}
}
