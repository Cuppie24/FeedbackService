using Application.EntityServices.Feedback;
using Application.EntityServices.Feedback.Dto;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Infrastructure.EntityServices.Feedback;

public class FeedbackRepo(MySqlDataSource dataSource, ILogger<FeedbackRepo> logger) : IFeedbackRepo
{
    // snake_case columns map onto the PascalCase POCO via
    // DefaultTypeMap.MatchNamesWithUnderscores (set in DependencyInjection).
    private const string SelectColumns =
        "id, title, assignee_id, user_id, type_id, status_id, system_id, created_at, updated_at";

    private const string InsertReturningIdSql =
        """
        INSERT INTO feedback
            (title, assignee_id, user_id, type_id, status_id, system_id, created_at, updated_at)
        VALUES
            (@Title, @AssigneeId, @UserId, @TypeId, @StatusId, @SystemId, @CreatedAt, @UpdatedAt);
        SELECT LAST_INSERT_ID();
        """;

    public async Task<Domain.Entities.Feedback?> GetAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Domain.Entities.Feedback>(
            $"SELECT {SelectColumns} FROM feedback WHERE id = @id",
            new { id });
    }

    public async Task<List<Domain.Entities.Feedback>> GetAsync(FeedbackFilter filter)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.Ids.Count > 0)
        {
            conditions.Add("id IN @Ids");
            parameters.Add("Ids", filter.Ids);
        }

        if (filter.StatusIds.Count > 0)
        {
            conditions.Add("status_id IN @StatusIds");
            parameters.Add("StatusIds", filter.StatusIds);
        }

        if (filter.TypeIds.Count > 0)
        {
            conditions.Add("type_id IN @TypeIds");
            parameters.Add("TypeIds", filter.TypeIds);
        }

        if (filter.AssigneeIds.Count > 0)
        {
            conditions.Add("assignee_id IN @AssigneeIds");
            parameters.Add("AssigneeIds", filter.AssigneeIds);
        }

        if (filter.SystemIds.Count > 0)
        {
            conditions.Add("system_id IN @SystemIds");
            parameters.Add("SystemIds", filter.SystemIds);
        }

        // Tags live in the feedback_tags join table, so match by existence rather
        // than a JOIN (a feedback with two matching tags must not come back twice).
        if (filter.TagIds.Count > 0)
        {
            conditions.Add(
                "EXISTS (SELECT 1 FROM feedback_tags AS ft WHERE ft.feedback_id = f.id AND ft.tag_id IN @TagIds)");
            parameters.Add("TagIds", filter.TagIds);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("title LIKE @Search");
            parameters.Add("Search", $"%{filter.Search.Trim()}%");
        }

        if (filter.FromDate is not null)
        {
            conditions.Add("created_at >= @FromDate");
            parameters.Add("FromDate", filter.FromDate);
        }

        if (filter.ToDate is not null)
        {
            conditions.Add("created_at <= @ToDate");
            parameters.Add("ToDate", filter.ToDate);
        }

        var sql = $"SELECT {SelectColumns} FROM feedback AS f";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);
        sql += " ORDER BY created_at DESC, id DESC";

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<Domain.Entities.Feedback>(sql, parameters);
        return rows.AsList();
    }

    public async Task<Domain.Entities.Feedback?> CreateAsync(Domain.Entities.Feedback feedback)
    {
        var now = DateTime.UtcNow;
        feedback.CreatedAt = now;
        feedback.UpdatedAt = now;

        await using var connection = await dataSource.OpenConnectionAsync();
        try
        {
            feedback.Id = await connection.ExecuteScalarAsync<int>(InsertReturningIdSql, feedback);
            return feedback;
        }
        catch (MySqlException e)
        {
            logger.LogError(e, "Failed to create feedback for user {UserId}", feedback.UserId);
            return null;
        }
    }

    public async Task UpdateAsync(FeedbackUpdateDto updateDto)
    {
        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", updateDto.Id);

        if (updateDto.Title is not null)
        {
            setClauses.Add("title = @Title");
            parameters.Add("Title", updateDto.Title);
        }

        // One interface method covers both the author edit (title only) and the
        // agent edit; the status/type columns ride in on the derived DTO.
        if (updateDto is FeedbackAgentUpdateDto agentDto)
        {
            if (agentDto.StatusId is not null)
            {
                setClauses.Add("status_id = @StatusId");
                parameters.Add("StatusId", agentDto.StatusId);
            }

            if (agentDto.TypeId is not null)
            {
                setClauses.Add("type_id = @TypeId");
                parameters.Add("TypeId", agentDto.TypeId);
            }
        }

        if (setClauses.Count == 0)
        {
            logger.LogWarning("UpdateAsync called for feedback {FeedbackId} with no fields to update", updateDto.Id);
            return;
        }

        setClauses.Add("updated_at = @UpdatedAt");
        parameters.Add("UpdatedAt", DateTime.UtcNow);

        await using var connection = await dataSource.OpenConnectionAsync();
        var affected = await connection.ExecuteAsync(
            $"UPDATE feedback SET {string.Join(", ", setClauses)} WHERE id = @Id",
            parameters);

        if (affected == 0)
            logger.LogWarning("UpdateAsync affected no rows: feedback {FeedbackId} does not exist", updateDto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        // feedback_messages / feedback_votes / feedback_status_history / feedback_tags
        // are ON DELETE CASCADE, so the child rows go with this one statement.
        await using var connection = await dataSource.OpenConnectionAsync();
        var affected = await connection.ExecuteAsync("DELETE FROM feedback WHERE id = @id", new { id });

        if (affected == 0)
            logger.LogWarning("DeleteAsync affected no rows: feedback {FeedbackId} does not exist", id);
    }
}
