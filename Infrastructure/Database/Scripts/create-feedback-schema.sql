-- =============================================================================
--  FeedbackService - entity table bootstrap (MySQL 8.0.1+ / 9.x, InnoDB)
-- =============================================================================
--  Column conventions (match the Domain.Entities POCOs read by Dapper):
--    * snake_case table and column names
--    * DateTime -> datetime(6); bool -> tinyint(1); int -> signed int
--    * created_at / updated_at / is_revoked have NO database default - the
--      repositories set them, so any other writer must supply them explicitly.
--
--  NOT created here - pre-existing shared tables in the external `refers` schema:
--    * User      -> `refers`.`m_user`   (UserId, UserName, PW)
--    * AppSystem -> `refers`.`m_system` (MSysId, MSysName)
--  Every FK into "users" / "apps" below targets `refers`.`m_user`.`UserId` or
--  `refers`.`m_system`.`MSysId` directly; `refers` must be on the same server
--  instance. The repositories also query `refers.m_user` directly (Dapper), so
--  no bridging views are needed. If cross-database FKs are not acceptable in
--  your environment, drop the nine FKs marked "-> refers." below and keep the
--  columns + their ix_* indexes; integrity is then enforced by the app.
--
--  Delete behaviour:
--    RESTRICT - FKs into lookup tables (type/status/role/tag) and into users,
--               to protect feedback content and audit trails.
--    CASCADE  - genuine parent -> owned child: feedback -> messages/votes/
--               history/tags, message -> attachments, app/agent/role link rows,
--               user -> refresh tokens, token -> replacement token.
--
--  Tables are created in FK-dependency order; run the whole file in one pass.
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `feedback`
    CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  Lookup tables (no outbound foreign keys)
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`roles` (
    `id`         int          NOT NULL AUTO_INCREMENT,
    `name`       varchar(100) NOT NULL,
    `code`       varchar(50)  NOT NULL,
    `created_at` datetime(6)  NOT NULL,
    `updated_at` datetime(6)  NOT NULL,
    CONSTRAINT `pk_roles` PRIMARY KEY (`id`),
    UNIQUE KEY `ix_roles_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `feedback`.`tags` (
    `id`         int          NOT NULL AUTO_INCREMENT,
    `name`       varchar(100) NOT NULL,
    `code`       varchar(50)  NOT NULL,
    `created_at` datetime(6)  NOT NULL,
    `updated_at` datetime(6)  NOT NULL,
    CONSTRAINT `pk_tags` PRIMARY KEY (`id`),
    UNIQUE KEY `ix_tags_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `feedback`.`feedback_types` (
    `id`         int          NOT NULL AUTO_INCREMENT,
    `name`       varchar(100) NOT NULL,
    `code`       varchar(50)  NOT NULL,
    `created_at` datetime(6)  NOT NULL,
    `updated_at` datetime(6)  NOT NULL,
    CONSTRAINT `pk_feedback_types` PRIMARY KEY (`id`),
    UNIQUE KEY `ix_feedback_types_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `feedback`.`feedback_statuses` (
    `id`         int         NOT NULL AUTO_INCREMENT,
    `name`       varchar(50) NOT NULL,
    `code`       varchar(50) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `pk_feedback_statuses` PRIMARY KEY (`id`),
    UNIQUE KEY `ix_feedback_statuses_code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  Agent  ->  refers.m_user
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`agents` (
    `id`         int         NOT NULL AUTO_INCREMENT,
    `user_id`    int         NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `pk_agents` PRIMARY KEY (`id`),
    KEY `ix_agents_user_id` (`user_id`),
    CONSTRAINT `fk_agents_m_user_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  AppAgent  ->  refers.m_system (apps),  feedback.agents
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`app_agents` (
    `id`         int         NOT NULL AUTO_INCREMENT,
    `app_id`     int         NOT NULL,
    `agent_id`   int         NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `pk_app_agents` PRIMARY KEY (`id`),
    KEY `ix_app_agents_app_id` (`app_id`),
    KEY `ix_app_agents_agent_id` (`agent_id`),
    CONSTRAINT `fk_app_agents_m_system_app_id` FOREIGN KEY (`app_id`)
        REFERENCES `refers`.`m_system` (`MSysId`) ON DELETE CASCADE,
    CONSTRAINT `fk_app_agents_agents_agent_id` FOREIGN KEY (`agent_id`)
        REFERENCES `feedback`.`agents` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  AppAgentRole  ->  feedback.app_agents,  feedback.roles
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`app_agent_roles` (
    `id`           int         NOT NULL AUTO_INCREMENT,
    `app_agent_id` int         NOT NULL,
    `role_id`      int         NOT NULL,
    `created_at`   datetime(6) NOT NULL,
    `updated_at`   datetime(6) NOT NULL,
    CONSTRAINT `pk_app_agent_roles` PRIMARY KEY (`id`),
    KEY `ix_app_agent_roles_app_agent_id` (`app_agent_id`),
    KEY `ix_app_agent_roles_role_id` (`role_id`),
    CONSTRAINT `fk_app_agent_roles_app_agents_app_agent_id` FOREIGN KEY (`app_agent_id`)
        REFERENCES `feedback`.`app_agents` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_app_agent_roles_roles_role_id` FOREIGN KEY (`role_id`)
        REFERENCES `feedback`.`roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  Feedback  ->  refers.m_user (assignee + author),  feedback_types,
--                feedback_statuses,  refers.m_system
--  DbSet "Feedbacks" is renamed to the singular table `feedback` via ToTable.
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`feedback` (
    `id`          int          NOT NULL AUTO_INCREMENT,
    `title`       varchar(500) NOT NULL,
    `assignee_id` int          NULL,
    `user_id`     int          NOT NULL,
    `type_id`     int          NOT NULL,
    `status_id`   int          NOT NULL,
    `system_id`   int          NOT NULL,
    `created_at`  datetime(6)  NOT NULL,
    `updated_at`  datetime(6)  NOT NULL,
    CONSTRAINT `pk_feedback` PRIMARY KEY (`id`),
    KEY `ix_feedback_assignee_id` (`assignee_id`),
    KEY `ix_feedback_user_id` (`user_id`),
    KEY `ix_feedback_type_id` (`type_id`),
    KEY `ix_feedback_status_id` (`status_id`),
    KEY `ix_feedback_system_id` (`system_id`),
    CONSTRAINT `fk_feedback_m_user_assignee_id` FOREIGN KEY (`assignee_id`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `fk_feedback_m_user_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `fk_feedback_feedback_types_type_id` FOREIGN KEY (`type_id`)
        REFERENCES `feedback`.`feedback_types` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_feedback_feedback_statuses_status_id` FOREIGN KEY (`status_id`)
        REFERENCES `feedback`.`feedback_statuses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_feedback_m_system_system_id` FOREIGN KEY (`system_id`)
        REFERENCES `refers`.`m_system` (`MSysId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  FeedbackMessage  ->  refers.m_user,  feedback.feedback
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`feedback_messages` (
    `id`          int            NOT NULL AUTO_INCREMENT,
    `text`        varchar(10000) NOT NULL,
    `user_id`     int            NOT NULL,
    `feedback_id` int            NOT NULL,
    `sent_at`     datetime(6)    NOT NULL,
    `created_at`  datetime(6)    NOT NULL,
    `updated_at`  datetime(6)    NOT NULL,
    CONSTRAINT `pk_feedback_messages` PRIMARY KEY (`id`),
    KEY `ix_feedback_messages_user_id` (`user_id`),
    KEY `ix_feedback_messages_feedback_id` (`feedback_id`),
    CONSTRAINT `fk_feedback_messages_m_user_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE RESTRICT,
    CONSTRAINT `fk_feedback_messages_feedback_feedback_id` FOREIGN KEY (`feedback_id`)
        REFERENCES `feedback`.`feedback` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  FeedbackAttachment  ->  feedback.feedback_messages
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`feedback_attachments` (
    `id`         int         NOT NULL AUTO_INCREMENT,
    `path`       longtext     NULL,
    `message_id` int         NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `pk_feedback_attachments` PRIMARY KEY (`id`),
    KEY `ix_feedback_attachments_message_id` (`message_id`),
    CONSTRAINT `fk_feedback_attachments_feedback_messages_message_id` FOREIGN KEY (`message_id`)
        REFERENCES `feedback`.`feedback_messages` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  FeedbackVote  ->  feedback.feedback,  refers.m_user
--  One vote per (feedback, user) - unique composite index.
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`feedback_votes` (
    `id`          int         NOT NULL AUTO_INCREMENT,
    `feedback_id` int         NOT NULL,
    `user_id`     int         NOT NULL,
    `created_at`  datetime(6) NOT NULL,
    `updated_at`  datetime(6) NOT NULL,
    CONSTRAINT `pk_feedback_votes` PRIMARY KEY (`id`),
    UNIQUE KEY `ix_feedback_votes_feedback_id_user_id` (`feedback_id`, `user_id`),
    KEY `ix_feedback_votes_user_id` (`user_id`),
    CONSTRAINT `fk_feedback_votes_feedback_feedback_id` FOREIGN KEY (`feedback_id`)
        REFERENCES `feedback`.`feedback` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_feedback_votes_m_user_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  FeedbackStatusHistory  ->  feedback.feedback,  feedback.feedback_statuses,
--                             refers.m_user
--  DbSet "FeedbackStatusHistories" -> table `feedback_status_history`;
--  property ChangerId -> column `changed_by` (both via explicit config).
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`feedback_status_history` (
    `id`          int         NOT NULL AUTO_INCREMENT,
    `feedback_id` int         NOT NULL,
    `status_id`   int         NOT NULL,
    `changed_by`  int         NOT NULL,
    `created_at`  datetime(6) NOT NULL,
    `updated_at`  datetime(6) NOT NULL,
    CONSTRAINT `pk_feedback_status_history` PRIMARY KEY (`id`),
    KEY `ix_feedback_status_history_feedback_id` (`feedback_id`),
    KEY `ix_feedback_status_history_status_id` (`status_id`),
    KEY `ix_feedback_status_history_changed_by` (`changed_by`),
    CONSTRAINT `fk_feedback_status_history_feedback_feedback_id` FOREIGN KEY (`feedback_id`)
        REFERENCES `feedback`.`feedback` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_feedback_status_history_feedback_statuses_status_id` FOREIGN KEY (`status_id`)
        REFERENCES `feedback`.`feedback_statuses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_feedback_status_history_m_user_changed_by` FOREIGN KEY (`changed_by`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  FeedbackTag  ->  feedback.feedback,  feedback.tags
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`feedback_tags` (
    `id`          int         NOT NULL AUTO_INCREMENT,
    `tag_id`      int         NOT NULL,
    `feedback_id` int         NOT NULL,
    `created_at`  datetime(6) NOT NULL,
    `updated_at`  datetime(6) NOT NULL,
    CONSTRAINT `pk_feedback_tags` PRIMARY KEY (`id`),
    KEY `ix_feedback_tags_tag_id` (`tag_id`),
    KEY `ix_feedback_tags_feedback_id` (`feedback_id`),
    CONSTRAINT `fk_feedback_tags_feedback_feedback_id` FOREIGN KEY (`feedback_id`)
        REFERENCES `feedback`.`feedback` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_feedback_tags_tags_tag_id` FOREIGN KEY (`tag_id`)
        REFERENCES `feedback`.`tags` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  RefreshToken  ->  refers.m_user,  self (ReplacedByToken, one-to-one)
-- -----------------------------------------------------------------------------

CREATE TABLE `feedback`.`refresh_tokens` (
    `id`                   int          NOT NULL AUTO_INCREMENT,
    `token`                varchar(255) NOT NULL,
    `user_id`              int          NOT NULL,
    `expires_at`           datetime(6)  NOT NULL,
    `replaced_by_token_id` int          NULL,
    `is_revoked`           tinyint(1)   NOT NULL,
    `revoked_at`           datetime(6)  NULL,
    `created_at`           datetime(6)  NOT NULL,
    `updated_at`           datetime(6)  NOT NULL,
    CONSTRAINT `pk_refresh_tokens` PRIMARY KEY (`id`),
    UNIQUE KEY `ix_refresh_tokens_token` (`token`),
    UNIQUE KEY `ix_refresh_tokens_replaced_by_token_id` (`replaced_by_token_id`),
    KEY `ix_refresh_tokens_user_id` (`user_id`),
    CONSTRAINT `fk_refresh_tokens_m_user_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `refers`.`m_user` (`UserId`) ON DELETE CASCADE,
    CONSTRAINT `fk_refresh_tokens_refresh_tokens_replaced_by_token_id` FOREIGN KEY (`replaced_by_token_id`)
        REFERENCES `feedback`.`refresh_tokens` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- =============================================================================
--  Teardown (reverse dependency order) - uncomment to drop everything.
-- =============================================================================
-- DROP TABLE IF EXISTS `feedback`.`refresh_tokens`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_tags`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_status_history`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_votes`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_attachments`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_messages`;
-- DROP TABLE IF EXISTS `feedback`.`feedback`;
-- DROP TABLE IF EXISTS `feedback`.`app_agent_roles`;
-- DROP TABLE IF EXISTS `feedback`.`app_agents`;
-- DROP TABLE IF EXISTS `feedback`.`agents`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_statuses`;
-- DROP TABLE IF EXISTS `feedback`.`feedback_types`;
-- DROP TABLE IF EXISTS `feedback`.`tags`;
-- DROP TABLE IF EXISTS `feedback`.`roles`;
