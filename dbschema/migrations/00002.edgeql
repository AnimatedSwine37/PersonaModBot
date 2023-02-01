CREATE MIGRATION m1kystsilqhyeofjvdymqsyaqggsyspyexpkugbov545xpenl35iwa
    ONTO m1genq4i4fdtfyded5ikh2bx3dxwdknf4jxbdowm4kcof4mielmqvq
{
  ALTER TYPE default::ForumConfig {
      CREATE REQUIRED PROPERTY postTip -> std::bool {
          SET REQUIRED USING (false);
      };
      CREATE PROPERTY tipMessage -> std::str;
  };
};
