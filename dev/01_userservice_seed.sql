BEGIN;

DELETE FROM refresh_token WHERE user_id IN (SELECT id FROM "user" WHERE email LIKE '%@test.com' OR email = 'den@den.com');
DELETE FROM subscription WHERE follower_id IN (SELECT id FROM "user" WHERE email LIKE '%@test.com' OR email = 'den@den.com');
DELETE FROM "user" WHERE email LIKE '%@test.com' OR email = 'den@den.com';

INSERT INTO "user" (id, username, email, password_hash, avatar_url, bio, is_verified, followers_count, following_count, created_at, updated_at, deleted_at, role) VALUES
  ('585c34fc-5261-57cf-a48e-5acb284628bf', 'ddenis22072006@gmail.com', 'ddenis22072006@gmail.com', '$2a$11$UzeHxTOgTN7jjp3c8wxD..Ss8xJACbKibaiyIp5KNWD7dt4UhHZ3C', 'https://i.pravatar.cc/150?u=den', 'Building GoydaGram.', TRUE, 3, 3, NOW() - INTERVAL '30 days', NOW() - INTERVAL '29 days', NULL, 'Admin'),
  ('585ae9d3-7cc8-5564-867d-0bd5d99647a0', 'alice', 'alice@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', 'https://i.pravatar.cc/150?u=alice', 'Just here for the memes.', TRUE, 2, 3, NOW() - INTERVAL '33 days', NOW() - INTERVAL '32 days', NULL, 'User'),
  ('c14eed81-5191-5f7b-920f-f94a8705fb3a', 'bob', 'bob@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', NULL, 'Gamer, streamer, night owl.', FALSE, 0, 2, NOW() - INTERVAL '36 days', NOW() - INTERVAL '35 days', NULL, 'User'),
  ('f493520e-bc84-5467-aac6-b5a00d4ec20e', 'carol', 'carol@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', 'https://i.pravatar.cc/150?u=carol', 'Food & travel content.', TRUE, 4, 1, NOW() - INTERVAL '39 days', NOW() - INTERVAL '38 days', NULL, 'User'),
  ('443adbb9-01a7-517f-98d7-18f84d88d7a8', 'dave', 'dave@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', NULL, NULL, FALSE, 0, 1, NOW() - INTERVAL '42 days', NOW() - INTERVAL '41 days', NULL, 'User'),
  ('b9289193-7755-59ca-9466-a7b64e45b14d', 'erin', 'erin@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', 'https://i.pravatar.cc/150?u=erin', 'Fitness coach.', FALSE, 1, 2, NOW() - INTERVAL '45 days', NOW() - INTERVAL '44 days', NULL, 'User'),
  ('6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', 'frank', 'frank@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', 'https://i.pravatar.cc/150?u=frank', 'Tech reviews and tutorials.', TRUE, 7, 1, NOW() - INTERVAL '48 days', NOW() - INTERVAL '47 days', NULL, 'User'),
  ('fdd45d33-916f-5dee-901b-4714483480a8', 'grace', 'grace@test.com', '$2b$11$273Z1htRNiJiEaeDAH1reuZAfbOFP10M4Z2J6hULrNidsZbQdLZcW', NULL, NULL, FALSE, 0, 4, NOW() - INTERVAL '51 days', NOW() - INTERVAL '50 days', NULL, 'User');

INSERT INTO subscription (id, follower_id, followee_id, created_at) VALUES
  ('d761e166-28dd-5acd-9515-60166bf47999', '585c34fc-5261-57cf-a48e-5acb284628bf', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '21 days'),
  ('5310a394-450f-5141-a03a-4a1906ef946e', '585c34fc-5261-57cf-a48e-5acb284628bf', 'f493520e-bc84-5467-aac6-b5a00d4ec20e', NOW() - INTERVAL '4 days'),
  ('89459e13-e1bc-5b94-9710-7f6b215e1401', '585c34fc-5261-57cf-a48e-5acb284628bf', '585ae9d3-7cc8-5564-867d-0bd5d99647a0', NOW() - INTERVAL '1 days'),
  ('956f4b74-199e-549c-80b5-f28f1c58421b', '585ae9d3-7cc8-5564-867d-0bd5d99647a0', '585c34fc-5261-57cf-a48e-5acb284628bf', NOW() - INTERVAL '24 days'),
  ('9aa2314a-5d4c-5d1c-9033-1e8ded06f09e', '585ae9d3-7cc8-5564-867d-0bd5d99647a0', 'f493520e-bc84-5467-aac6-b5a00d4ec20e', NOW() - INTERVAL '9 days'),
  ('4f08dac4-07f7-5226-8f4e-f28e2a8c2ca7', '585ae9d3-7cc8-5564-867d-0bd5d99647a0', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '8 days'),
  ('ff0a8298-3319-5b60-9f2a-982f6b28a758', 'c14eed81-5191-5f7b-920f-f94a8705fb3a', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '8 days'),
  ('b8bfcdf3-1530-5605-aac8-49bb38a8aa65', 'c14eed81-5191-5f7b-920f-f94a8705fb3a', 'b9289193-7755-59ca-9466-a7b64e45b14d', NOW() - INTERVAL '5 days'),
  ('e3bb763e-34e2-55ec-9cdb-7032eeb955a6', 'f493520e-bc84-5467-aac6-b5a00d4ec20e', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '24 days'),
  ('45b22b30-ca67-58b3-bc40-f92887f81891', 'b9289193-7755-59ca-9466-a7b64e45b14d', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '4 days'),
  ('002c42c3-5857-5635-980f-83092e598b78', 'b9289193-7755-59ca-9466-a7b64e45b14d', 'f493520e-bc84-5467-aac6-b5a00d4ec20e', NOW() - INTERVAL '22 days'),
  ('47d8ff47-244b-5679-b55d-2c790254ffcf', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', '585c34fc-5261-57cf-a48e-5acb284628bf', NOW() - INTERVAL '24 days'),
  ('77751f2e-1981-5290-bfbd-e6bd9b382b98', 'fdd45d33-916f-5dee-901b-4714483480a8', '585c34fc-5261-57cf-a48e-5acb284628bf', NOW() - INTERVAL '18 days'),
  ('6766af6a-0399-52c5-9acb-5e1a2b08116a', 'fdd45d33-916f-5dee-901b-4714483480a8', '585ae9d3-7cc8-5564-867d-0bd5d99647a0', NOW() - INTERVAL '3 days'),
  ('452cd76b-a485-5b0b-ace6-76065ea43342', 'fdd45d33-916f-5dee-901b-4714483480a8', 'f493520e-bc84-5467-aac6-b5a00d4ec20e', NOW() - INTERVAL '19 days'),
  ('f32b2107-8c3e-5398-8386-bde87428011f', 'fdd45d33-916f-5dee-901b-4714483480a8', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '14 days'),
  ('0d77f2fd-7583-58db-8f92-9fad2b431819', '443adbb9-01a7-517f-98d7-18f84d88d7a8', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', NOW() - INTERVAL '2 days');

INSERT INTO refresh_token (id, user_id, token_hash, created_at, expires_at, revoked_at) VALUES
  ('3739531b-888e-546c-9ab6-0f495b5a4144', '585c34fc-5261-57cf-a48e-5acb284628bf', '72c9e870df98938d5f140e44347daab28d8b2b22cab17dec70d738fac40df298', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('3bbed2fd-6a73-5b6e-9ade-2be42e3af1e3', '585ae9d3-7cc8-5564-867d-0bd5d99647a0', 'ec72438cc84024774a7c759a45ea2351b06a4439140b95bacc706f10620a7dee', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('d41374ed-1005-51e2-8420-2a916140cd97', 'c14eed81-5191-5f7b-920f-f94a8705fb3a', '531ab212fabc122563f68f7da5d1a55e9cdcf685c30693d3ce5acad083234615', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('e24774e6-146f-5b4e-a2b8-d1fce274c017', 'f493520e-bc84-5467-aac6-b5a00d4ec20e', '84ecb000834edd04c63f2cb578d273f68255968be6abf7531593c526b4378510', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('5467c2f5-0bf1-525b-88fe-c0f29b92ffde', '443adbb9-01a7-517f-98d7-18f84d88d7a8', 'c6064fa5a28a4f7ab90ed0c54cecd0d1a7f815b87275bf696885cdc0d3259c49', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('b7aae007-797f-58ac-9e79-8bad275d79be', 'b9289193-7755-59ca-9466-a7b64e45b14d', 'b8bc7c1c9edeb1ad1bf281e9ea22657ccfb1d4723e2e9570b6ef2b77224058a2', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('03bce4c4-c191-5359-a075-6bbc8693eceb', '6e05602e-4bdd-5d7a-8eaa-6cbad25834a9', '497cda8ced5d31e27abbefaa7ab150edf0475c7f7a8eb8f6502563537fc6cc84', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL),
  ('0d28251a-7b57-594c-abd7-922cd45c72ee', 'fdd45d33-916f-5dee-901b-4714483480a8', 'eaa82b306f83c23843d33cb3ac9e3a61edc9efb9aa233ca6ea00a5616e407367', NOW() - INTERVAL '1 day', NOW() + INTERVAL '13 days', NULL);

COMMIT;