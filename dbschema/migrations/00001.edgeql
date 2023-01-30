CREATE MIGRATION m1genq4i4fdtfyded5ikh2bx3dxwdknf4jxbdowm4kcof4mielmqvq
    ONTO initial
{
  CREATE FUTURE nonrecursive_access_policies;
  CREATE TYPE default::RoleConfig {
      CREATE REQUIRED PROPERTY allowRename -> std::bool;
      CREATE REQUIRED PROPERTY allowSolve -> std::bool;
      CREATE REQUIRED PROPERTY allowTag -> std::bool;
      CREATE REQUIRED PROPERTY roleId -> std::int64;
  };
  CREATE TYPE default::ForumConfig {
      CREATE REQUIRED MULTI LINK allowedRoles -> default::RoleConfig {
          CREATE CONSTRAINT std::exclusive;
      };
      CREATE REQUIRED PROPERTY forumId -> std::int64 {
          CREATE CONSTRAINT std::exclusive;
      };
      CREATE REQUIRED PROPERTY solvedMessage -> std::str;
      CREATE REQUIRED PROPERTY solvedTag -> std::int64;
  };
  CREATE TYPE default::GuildConfig {
      CREATE REQUIRED MULTI LINK forumConfigs -> default::ForumConfig {
          CREATE CONSTRAINT std::exclusive;
      };
      CREATE REQUIRED PROPERTY guildId -> std::int64 {
          CREATE CONSTRAINT std::exclusive;
      };
  };
};
