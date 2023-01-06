CREATE MIGRATION m1dccbzme6edlq774odkwhu6fnpsqyvnbv6l6junvj6ipl2g5mo56a
    ONTO m1kzx5w4xvtqutgm5j4z7tvpy2df7nxyahntrspn7lqc5qj6dljn6q
{
  ALTER TYPE default::ForumConfig {
      ALTER PROPERTY AllowedRoles {
          RENAME TO allowedRoles;
      };
  };
};
