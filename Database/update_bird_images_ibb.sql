-- Update bird images to use ibb.co URLs
-- Run this script to update existing birds with working image URLs
-- These ibb.co URLs can then be migrated to S3 using the ImageMigrationTool

BEGIN;

-- Update Arabic birds with ibb.co URLs
UPDATE birds SET image_url = 'https://i.ibb.co/FLgN1vnD/Anas-platyrhynchos-in-Aveyron.jpg'
WHERE bird_id = 'aaaa1111-0002-0002-0002-000000000002'; -- سلطان (Drake)

UPDATE birds SET image_url = 'https://i.ibb.co/gLz0qk8c/Ross-s-Goose-Chen-rossii-23321411711.jpg'
WHERE bird_id = 'aaaa1111-0003-0003-0003-000000000003'; -- وزوز (White Goose)

UPDATE birds SET image_url = 'https://i.ibb.co/b5ynJzHC/Male-and-female-chicken-sitting-together.jpg'
WHERE bird_id = 'aaaa1111-0004-0004-0004-000000000004'; -- دجاجة (Local Hen)

UPDATE birds SET image_url = 'https://i.ibb.co/cK3mKVqg/Lone-Rooster.jpg'
WHERE bird_id = 'aaaa1111-0005-0005-0005-000000000005'; -- صياح (Rooster)

UPDATE birds SET image_url = 'https://i.ibb.co/kgmvNc6C/Poultry-Classes-Blog-photo-Flickr-USDAgov.jpg'
WHERE bird_id = 'aaaa1111-0006-0006-0006-000000000006'; -- سمين (Broiler)

UPDATE birds SET image_url = 'https://i.ibb.co/G4WygwGy/Mandarin-duck-arp.jpg'
WHERE bird_id = 'bbbb1111-0001-0001-0001-000000000001'; -- زهرة (Colored Duck)

UPDATE birds SET image_url = 'https://i.ibb.co/YT3T1k21/White-homing-pigeon.jpg'
WHERE bird_id = 'bbbb1111-0002-0002-0002-000000000002'; -- حمامة السلام (White Pigeon)

UPDATE birds SET image_url = 'https://i.ibb.co/fTLLbtd/Greylag-Goose-St-James-s-Park-London-Nov-2006.jpg'
WHERE bird_id = 'bbbb1111-0003-0003-0003-000000000003'; -- عنتر (Grey Goose)

UPDATE birds SET image_url = 'https://i.ibb.co/gFVtYQWv/Golden-Comet-Adult.webp'
WHERE bird_id = 'bbbb1111-0004-0004-0004-000000000004'; -- ذهبية (Golden Hen)

UPDATE birds SET image_url = 'https://i.ibb.co/TxCYdkw6/Chick.jpg'
WHERE bird_id = 'bbbb1111-0005-0005-0005-000000000005'; -- صوصو (Chick)

UPDATE birds SET image_url = 'https://i.ibb.co/HLh3Bg8R/Homing-pigeon.jpg'
WHERE bird_id = 'cccc1111-0001-0001-0001-000000000001'; -- زاجل (Homing Pigeon)

UPDATE birds SET image_url = 'https://i.ibb.co/0p0J87nk/Picture-of-a-pigeon-flying.jpg'
WHERE bird_id = 'cccc1111-0002-0002-0002-000000000002'; -- طيار (Flying Pigeon)

UPDATE birds SET image_url = 'https://i.ibb.co/yFc9YVg0/Collared-dove.jpg'
WHERE bird_id = 'cccc1111-0003-0003-0003-000000000003'; -- نجمة (Collared Dove)

UPDATE birds SET image_url = 'https://i.ibb.co/ccbNb2jc/Muscovy-Duck-Cairina-moschata-male-29039391935.jpg'
WHERE bird_id = 'cccc1111-0004-0004-0004-000000000004'; -- أمير (Muscovy Duck)

UPDATE birds SET image_url = 'https://i.ibb.co/8g2m8r1G/American-Black-Duck-male-RWD5.jpg'
WHERE bird_id = 'cccc1111-0005-0005-0005-000000000005'; -- لؤلؤة (Black Duck)

UPDATE birds SET image_url = 'https://i.ibb.co/Y7MQGPnF/Rhode-Island-Red-Hen.jpg'
WHERE bird_id = 'dddd1111-0001-0001-0001-000000000001'; -- معلمة (Red Hen)

UPDATE birds SET image_url = 'https://i.ibb.co/dsnTHxZb/Mourning-Dove-2006.jpg'
WHERE bird_id = 'dddd1111-0002-0002-0002-000000000002'; -- ورقاء (Dove)

UPDATE birds SET image_url = 'https://i.ibb.co/DfgXMLXW/Canada-Goose.webp'
WHERE bird_id = 'dddd1111-0003-0003-0003-000000000003'; -- حكيم (Canada Goose)

UPDATE birds SET image_url = 'https://i.ibb.co/qLypRYMG/Cornish-Cross.jpg'
WHERE bird_id = 'dddd1111-0004-0004-0004-000000000004'; -- كبير (White Broiler)

-- Update birds for abdelfattahragab@outlook.com user
UPDATE birds SET image_url = 'https://i.ibb.co/YT3T1k21/White-homing-pigeon.jpg'
WHERE bird_id = 'ffff1111-0001-0001-0001-000000000001'; -- سلامة (White Pigeon)

UPDATE birds SET image_url = 'https://i.ibb.co/cK3mKVqg/Lone-Rooster.jpg'
WHERE bird_id = 'ffff1111-0002-0002-0002-000000000002'; -- فارس (Rooster)

UPDATE birds SET image_url = 'https://i.ibb.co/G4WygwGy/Mandarin-duck-arp.jpg'
WHERE bird_id = 'ffff1111-0003-0003-0003-000000000003'; -- جميلة (Mandarin Duck)

-- Keep first bird (بطوطة) with Wikimedia URL that works
UPDATE birds SET image_url = 'https://upload.wikimedia.org/wikipedia/commons/3/3f/Amerikanische_Pekingenten_2013_01%2C_cropped.jpg'
WHERE bird_id = 'aaaa1111-0001-0001-0001-000000000001'; -- بطوطة (White Duck)

COMMIT;

-- Verify updates
SELECT bird_id, name, LEFT(image_url, 50) as image_url_preview FROM birds WHERE image_url LIKE 'https://i.ibb.co/%';
