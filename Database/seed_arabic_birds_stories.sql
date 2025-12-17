-- =============================================
-- Arabic Birds & Stories Seed Data
-- =============================================
-- This script adds:
-- - 4 Arabic-speaking users
-- - 20 birds (Duck, Pigeon, Goose, Hen, Drake, Chicken, Broiler)
-- - 40 stories (2 per bird)
-- =============================================

BEGIN;

-- =============================================
-- ARABIC USERS (4 users)
-- =============================================
INSERT INTO users (
    user_id, name, email, password_hash, profile_image, bio,
    created_at, email_confirmed, is_account_locked, failed_login_attempts,
    last_password_change_at
) VALUES
-- Ahmed (farm owner)
('aaaaaaaa-1111-2222-3333-444444444444',
 'Ahmed Al-Rashid',
 'ahmed@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'مربي طيور منذ 20 عاماً. أحب رعاية الدواجن والطيور المنزلية.',
 NOW() - INTERVAL '180 days',
 true, false, 0,
 NOW() - INTERVAL '180 days'),

-- Fatima (bird lover)
('bbbbbbbb-1111-2222-3333-444444444444',
 'Fatima Al-Zahra',
 'fatima@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'محبة للطيور وصاحبة مزرعة صغيرة في الريف.',
 NOW() - INTERVAL '120 days',
 true, false, 0,
 NOW() - INTERVAL '120 days'),

-- Omar (pigeon breeder)
('cccccccc-1111-2222-3333-444444444444',
 'Omar Hassan',
 'omar@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'مربي حمام محترف. أشارك تجاربي في تربية الحمام الزاجل.',
 NOW() - INTERVAL '90 days',
 true, false, 0,
 NOW() - INTERVAL '90 days'),

-- Layla (educator)
('dddddddd-1111-2222-3333-444444444444',
 'Layla Ibrahim',
 'layla@example.com',
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYNq8QRhXSS', -- Password123!
 NULL,
 'معلمة علوم. أستخدم الطيور لتعليم الأطفال عن الطبيعة.',
 NOW() - INTERVAL '60 days',
 true, false, 0,
 NOW() - INTERVAL '60 days')
ON CONFLICT (user_id) DO NOTHING;

-- =============================================
-- ARABIC BIRDS (20 birds)
-- =============================================
INSERT INTO birds (
    bird_id, owner_id, name, species, tagline, description, image_url,
    created_at, loved_count, supported_count, donation_cents,
    is_premium, is_memorial, max_media_count
) VALUES
-- Ahmed's Birds (6 birds)
-- Duck 1
('aaaa1111-0001-0001-0001-000000000001',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'بطوطة',
 'بطة بيضاء',
 'ملكة البركة',
 'بطوطة هي بطة بيضاء جميلة تعيش في بركة مزرعتنا. تحب السباحة في الصباح الباكر وتنادي رفاقها بصوتها المميز. أصبحت رمزاً للمزرعة.',
 'https://upload.wikimedia.org/wikipedia/commons/3/3f/Amerikanische_Pekingenten_2013_01%2C_cropped.jpg',
 NOW() - INTERVAL '150 days',
 45, 12, 4500,
 true, false, 20),

-- Drake 1
('aaaa1111-0002-0002-0002-000000000002',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'سلطان',
 'ذكر البط',
 'الحارس الأمين',
 'سلطان هو ذكر بط قوي وشجاع. يحرس عائلته من البط بكل إخلاص ويقودهم إلى أفضل أماكن الطعام في البركة.',
 'https://i.ibb.co/FLgN1vnD/Anas-platyrhynchos-in-Aveyron.jpg',
 NOW() - INTERVAL '145 days',
 38, 10, 3800,
 true, false, 20),

-- Goose 1
('aaaa1111-0003-0003-0003-000000000003',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'وزوز',
 'إوزة بيضاء',
 'حارس المزرعة',
 'وزوز إوزة قوية تحرس المزرعة بشجاعة. صوتها العالي ينبهنا لأي غريب يقترب. أصبحت أفضل حارس طبيعي للمكان.',
 'https://i.ibb.co/gLz0qk8c/Ross-s-Goose-Chen-rossii-23321411711.jpg',
 NOW() - INTERVAL '140 days',
 52, 15, 5200,
 true, false, 20),

-- Hen 1
('aaaa1111-0004-0004-0004-000000000004',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'دجاجة',
 'دجاجة بلدية',
 'أم الصيصان',
 'دجاجة أم حنونة ترعى صيصانها بكل حب. تجمعهم تحت جناحيها عند الخطر وتعلمهم البحث عن الطعام.',
 'https://i.ibb.co/b5ynJzHC/Male-and-female-chicken-sitting-together.jpg',
 NOW() - INTERVAL '135 days',
 35, 8, 3500,
 false, false, 10),

-- Chicken/Rooster 1
('aaaa1111-0005-0005-0005-000000000005',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'صياح',
 'ديك بلدي',
 'منبه الفجر',
 'صياح ديك جميل بريش ملون. يوقظنا كل صباح بصياحه القوي. أصبح صوته جزءاً من روتين المزرعة اليومي.',
 'https://i.ibb.co/cK3mKVqg/Lone-Rooster.jpg',
 NOW() - INTERVAL '130 days',
 42, 11, 4200,
 true, false, 20),

-- Broiler 1
('aaaa1111-0006-0006-0006-000000000006',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'سمين',
 'دجاج لاحم',
 'الكبير الصغير',
 'سمين فرخ لاحم نما بسرعة مذهلة. رغم حجمه الكبير، يظل لطيفاً ويحب أن نطعمه باليد.',
 'https://i.ibb.co/kgmvNc6C/Poultry-Classes-Blog-photo-Flickr-USDAgov.jpg',
 NOW() - INTERVAL '125 days',
 28, 6, 2800,
 false, false, 10),

-- Fatima's Birds (5 birds)
-- Duck 2
('bbbb1111-0001-0001-0001-000000000001',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'زهرة',
 'بطة ملونة',
 'جوهرة الماء',
 'زهرة بطة بريش ملون رائع. تزين بركتنا الصغيرة وتجذب أنظار كل من يزور المزرعة.',
 'https://i.ibb.co/G4WygwGy/Mandarin-duck-arp.jpg',
 NOW() - INTERVAL '100 days',
 55, 18, 5500,
 true, false, 20),

-- Pigeon 1
('bbbb1111-0002-0002-0002-000000000002',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'حمامة السلام',
 'حمامة بيضاء',
 'رمز الأمل',
 'حمامة بيضاء نقية كالثلج. نطلقها في المناسبات السعيدة. أصبحت رمزاً للسلام في حينا.',
 'https://i.ibb.co/YT3T1k21/White-homing-pigeon.jpg',
 NOW() - INTERVAL '95 days',
 68, 22, 6800,
 true, false, 20),

-- Goose 2
('bbbb1111-0003-0003-0003-000000000003',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'عنتر',
 'إوز رمادي',
 'القائد الشجاع',
 'عنتر إوز رمادي كبير يقود قطيعه بحكمة. يعرف أفضل المراعي ويحمي صغاره بشراسة.',
 'https://i.ibb.co/fTLLbtd/Greylag-Goose-St-James-s-Park-London-Nov-2006.jpg',
 NOW() - INTERVAL '90 days',
 40, 13, 4000,
 false, false, 10),

-- Hen 2
('bbbb1111-0004-0004-0004-000000000004',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'ذهبية',
 'دجاجة ذهبية',
 'البياضة الماهرة',
 'ذهبية دجاجة بريش ذهبي جميل. تبيض بيضاً طازجاً كل يوم وتحب التجول في الحديقة.',
 'https://i.ibb.co/gFVtYQWv/Golden-Comet-Adult.webp',
 NOW() - INTERVAL '85 days',
 32, 9, 3200,
 false, false, 10),

-- Chicken 2
('bbbb1111-0005-0005-0005-000000000005',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'صوصو',
 'صوص صغير',
 'الصغير الشقي',
 'صوصو صوص صغير أصفر لطيف. يتبع أمه أينما ذهبت ويحاول تقليدها في كل شيء.',
 'https://i.ibb.co/TxCYdkw6/Chick.jpg',
 NOW() - INTERVAL '80 days',
 75, 25, 7500,
 true, false, 20),

-- Omar's Birds (5 birds) - Pigeon specialist
-- Pigeon 2
('cccc1111-0001-0001-0001-000000000001',
 'cccccccc-1111-2222-3333-444444444444',
 'زاجل',
 'حمام زاجل',
 'الرسول الأمين',
 'زاجل حمام زاجل مدرب. يستطيع العودة إلى المنزل من مسافات بعيدة. فاز في عدة سباقات.',
 'https://i.ibb.co/HLh3Bg8R/Homing-pigeon.jpg',
 NOW() - INTERVAL '75 days',
 85, 30, 8500,
 true, false, 20),

-- Pigeon 3
('cccc1111-0002-0002-0002-000000000002',
 'cccccccc-1111-2222-3333-444444444444',
 'طيار',
 'حمام طيار',
 'سيد السماء',
 'طيار حمام يحلق عالياً في السماء. يؤدي حركات بهلوانية مذهلة أثناء طيرانه.',
 'https://i.ibb.co/0p0J87nk/Picture-of-a-pigeon-flying.jpg',
 NOW() - INTERVAL '70 days',
 62, 20, 6200,
 true, false, 20),

-- Pigeon 4
('cccc1111-0003-0003-0003-000000000003',
 'cccccccc-1111-2222-3333-444444444444',
 'نجمة',
 'حمامة مطوقة',
 'الجميلة الهادئة',
 'نجمة حمامة بطوق أبيض جميل حول رقبتها. هادئة وودودة، تحب أن نحملها.',
 'https://i.ibb.co/yFc9YVg0/Collared-dove.jpg',
 NOW() - INTERVAL '65 days',
 48, 14, 4800,
 false, false, 10),

-- Drake 2
('cccc1111-0004-0004-0004-000000000004',
 'cccccccc-1111-2222-3333-444444444444',
 'أمير',
 'بط مسكوفي',
 'الهادئ الوسيم',
 'أمير ذكر بط مسكوفي بوجه أحمر مميز. هادئ ولطيف مع الجميع رغم حجمه الكبير.',
 'https://i.ibb.co/ccbNb2jc/Muscovy-Duck-Cairina-moschata-male-29039391935.jpg',
 NOW() - INTERVAL '60 days',
 35, 10, 3500,
 false, false, 10),

-- Duck 3
('cccc1111-0005-0005-0005-000000000005',
 'cccccccc-1111-2222-3333-444444444444',
 'لؤلؤة',
 'بطة سوداء',
 'الغامضة الجميلة',
 'لؤلؤة بطة بريش أسود لامع. تبدو غامضة لكنها لطيفة جداً مع من يعرفها.',
 'https://i.ibb.co/8g2m8r1G/American-Black-Duck-male-RWD5.jpg',
 NOW() - INTERVAL '55 days',
 30, 8, 3000,
 false, false, 10),

-- Layla's Birds (4 birds) - Educational
-- Hen 3
('dddd1111-0001-0001-0001-000000000001',
 'dddddddd-1111-2222-3333-444444444444',
 'معلمة',
 'دجاجة حمراء',
 'صديقة الأطفال',
 'معلمة دجاجة لطيفة تحبها الأطفال في المدرسة. نستخدمها لتعليم الطلاب عن دورة حياة الدجاج.',
 'https://i.ibb.co/Y7MQGPnF/Rhode-Island-Red-Hen.jpg',
 NOW() - INTERVAL '50 days',
 58, 19, 5800,
 true, false, 20),

-- Pigeon 5
('dddd1111-0002-0002-0002-000000000002',
 'dddddddd-1111-2222-3333-444444444444',
 'ورقاء',
 'يمامة',
 'صوت الصباح',
 'ورقاء يمامة تزور حديقة المدرسة كل صباح. صوتها العذب يبدأ يومنا بجمال.',
 'https://i.ibb.co/dsnTHxZb/Mourning-Dove-2006.jpg',
 NOW() - INTERVAL '45 days',
 44, 12, 4400,
 false, false, 10),

-- Goose 3
('dddd1111-0003-0003-0003-000000000003',
 'dddddddd-1111-2222-3333-444444444444',
 'حكيم',
 'إوز كندي',
 'الزائر الموسمي',
 'حكيم إوزة كندية تزورنا كل شتاء. الطلاب ينتظرون وصولها بفارغ الصبر.',
 'https://i.ibb.co/DfgXMLXW/Canada-Goose.webp',
 NOW() - INTERVAL '40 days',
 50, 16, 5000,
 true, false, 20),

-- Broiler 2
('dddd1111-0004-0004-0004-000000000004',
 'dddddddd-1111-2222-3333-444444444444',
 'كبير',
 'فروج أبيض',
 'العملاق اللطيف',
 'كبير فروج ضخم لكنه لطيف جداً. نستخدمه لشرح كيف تنمو الدواجن للطلاب.',
 'https://i.ibb.co/qLypRYMG/Cornish-Cross.jpg',
 NOW() - INTERVAL '35 days',
 38, 11, 3800,
 false, false, 10)
ON CONFLICT (bird_id) DO NOTHING;

-- =============================================
-- STORIES (40 stories - 2 per bird)
-- =============================================
INSERT INTO stories (
    story_id, author_id, bird_id, content, mode, image_url, video_url, is_highlighted, created_at
) VALUES
-- بطوطة stories (Duck 1)
('aaaa1111-1001-1001-1001-000000000001',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0001-0001-0001-000000000001',
 'اليوم بطوطة سبحت لأول مرة مع صغارها الخمسة! كانت تقودهم بحنان في البركة وتعلمهم كيف يغوصون للبحث عن الطعام. منظر رائع لا يُنسى!',
 1, -- NewBeginning
 'https://upload.wikimedia.org/wikipedia/commons/1/14/Ducks_in_a_row.jpg',
 NULL,
 false,
 NOW() - INTERVAL '10 days'),

('aaaa1111-1002-1002-1002-000000000002',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0001-0001-0001-000000000001',
 'بطوطة بنت عشها الجديد قرب الشجرة الكبيرة. اختارت المكان بعناية ليكون قريباً من الماء وبعيداً عن الخطر. دجاجة ذكية فعلاً!',
 0, -- LoveAndBond
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '20 days'),

-- سلطان stories (Drake 1)
('aaaa1111-1003-1003-1003-000000000003',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0002-0002-0002-000000000002',
 'سلطان أظهر شجاعة كبيرة اليوم! طرد قطاً غريباً كان يقترب من البطات الصغيرة. وقف أمامه ونشر جناحيه حتى هرب القط.',
 2, -- ProgressAndWins
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '8 days'),

('aaaa1111-1004-1004-1004-000000000004',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0002-0002-0002-000000000002',
 'لاحظت اليوم أن سلطان يقود عائلته في نزهة صباحية حول البركة. يمشي في المقدمة ويتأكد من سلامة الطريق قبل أن يتبعه الآخرون.',
 7, -- DailyLife
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '15 days'),

-- وزوز stories (Goose 1)
('aaaa1111-1005-1005-1005-000000000005',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0003-0003-0003-000000000003',
 'وزوز أنقذت المزرعة الليلة! سمعت صوتها العالي في منتصف الليل فخرجت لأجد ثعلباً يحاول دخول قن الدجاج. شكراً يا وزوز!',
 2, -- ProgressAndWins
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '5 days'),

('aaaa1111-1006-1006-1006-000000000006',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0003-0003-0003-000000000003',
 'صباح هادئ مع وزوز. كانت تمشي بجانبي وأنا أطعم الطيور الأخرى. أصبحت صديقتي المقربة رغم مظهرها المخيف للغرباء.',
 4, -- PeacefulMoment
 'https://upload.wikimedia.org/wikipedia/commons/0/05/Domestic_goose_-_Toulouse.jpg',
 NULL,
 false,
 NOW() - INTERVAL '12 days'),

-- دجاجة stories (Hen 1)
('aaaa1111-1007-1007-1007-000000000007',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0004-0004-0004-000000000004',
 'دجاجة فقست بيضها اليوم! سبعة صيصان صغار أصفر جميلين. تجمعهم تحت جناحيها وتحميهم من البرد. قلب أم حقيقي!',
 1, -- NewBeginning
 'https://upload.wikimedia.org/wikipedia/commons/7/73/Hen_with_chicks.jpg',
 NULL,
 false,
 NOW() - INTERVAL '3 days'),

('aaaa1111-1008-1008-1008-000000000008',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0004-0004-0004-000000000004',
 'شاهدت دجاجة تعلم صيصانها كيف ينقرون الحبوب اليوم. تأخذ حبة وتضعها أمامهم وتنقرها ببطء ليقلدوها. معلمة ماهرة!',
 0, -- LoveAndBond
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '18 days'),

-- صياح stories (Rooster 1)
('aaaa1111-1009-1009-1009-000000000009',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0005-0005-0005-000000000005',
 'صياح بدأ يصيح اليوم لأول مرة! صوته لم يكن قوياً في البداية لكنه تحسن بسرعة. الآن يوقظ الجميع في الفجر بصياحه الجميل.',
 2, -- ProgressAndWins
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '7 days'),

('aaaa1111-1010-1010-1010-000000000010',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0005-0005-0005-000000000005',
 'موقف مضحك مع صياح! حاول أن يصيح وهو واقف على سياج عالٍ فسقط منه. نهض بسرعة ونفض ريشه كأن شيئاً لم يحدث.',
 3, -- FunnyMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '14 days'),

-- سمين stories (Broiler 1)
('aaaa1111-1011-1011-1011-000000000011',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0006-0006-0006-000000000006',
 'سمين يكبر بسرعة مذهلة! في أسبوع واحد فقط زاد وزنه كثيراً. يحب أن يأكل من يدي ويتبعني في كل مكان.',
 7, -- DailyLife
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '6 days'),

('aaaa1111-1012-1012-1012-000000000012',
 'aaaaaaaa-1111-2222-3333-444444444444',
 'aaaa1111-0006-0006-0006-000000000006',
 'اليوم أخذت سمين للفحص الصحي. الطبيب قال إنه بصحة ممتازة ونموه طبيعي. مرتاح على صحته!',
 6, -- CareAndHealth
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '16 days'),

-- زهرة stories (Duck 2)
('bbbb1111-1001-1001-1001-000000000001',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0001-0001-0001-000000000001',
 'زهرة بريشها الملون تسحر كل من يراها! اليوم جاء زوار للمزرعة وكلهم وقفوا يصورونها. أصبحت نجمة حقيقية!',
 3, -- FunnyMoment
 'https://upload.wikimedia.org/wikipedia/commons/b/b0/Anas_platyrhynchos_-United_Kingdom_-male-8a.jpg',
 NULL,
 false,
 NOW() - INTERVAL '4 days'),

('bbbb1111-1002-1002-1002-000000000002',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0001-0001-0001-000000000001',
 'زهرة تغطس في الماء البارد حتى في الشتاء! تبدو سعيدة جداً وهي تسبح بين قطع الجليد الصغيرة. بطة شجاعة!',
 4, -- PeacefulMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '11 days'),

-- حمامة السلام stories (Pigeon 1)
('bbbb1111-1003-1003-1003-000000000003',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0002-0002-0002-000000000002',
 'أطلقنا حمامة السلام في حفل زفاف ابنة الجيران! طارت عالياً في السماء الزرقاء ثم عادت إلى المنزل بأمان. لحظة مؤثرة للجميع.',
 0, -- LoveAndBond
 'https://upload.wikimedia.org/wikipedia/commons/e/e4/White_dove_-_male.jpg',
 NULL,
 false,
 NOW() - INTERVAL '2 days'),

('bbbb1111-1004-1004-1004-000000000004',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0002-0002-0002-000000000002',
 'حمامة السلام باضت بيضتين! ننتظر بفارغ الصبر أن يفقس الصغار. ستكون عائلة جميلة من الحمام الأبيض.',
 1, -- NewBeginning
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '19 days'),

-- عنتر stories (Goose 2)
('bbbb1111-1005-1005-1005-000000000005',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0003-0003-0003-000000000003',
 'عنتر قاد قطيعه اليوم إلى مرعى جديد اكتشفته! مشى في المقدمة وتبعته عشر إوزات. قائد حكيم فعلاً.',
 2, -- ProgressAndWins
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '9 days'),

('bbbb1111-1006-1006-1006-000000000006',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0003-0003-0003-000000000003',
 'عنتر يستحم في البركة! يرش الماء على ريشه بسعادة ثم ينفضه بقوة. يقضي وقتاً طويلاً في تنظيف نفسه.',
 4, -- PeacefulMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '17 days'),

-- ذهبية stories (Hen 2)
('bbbb1111-1007-1007-1007-000000000007',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0004-0004-0004-000000000004',
 'ذهبية باضت بيضة كبيرة جداً اليوم! أكبر من المعتاد بكثير. فحصناها ووجدنا أنها بصفارين. شيء نادر ومميز!',
 3, -- FunnyMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '1 day'),

('bbbb1111-1008-1008-1008-000000000008',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0004-0004-0004-000000000004',
 'ذهبية تتجول في الحديقة وتأكل الحشرات الضارة. خدمة مجانية للبستنة! أفضل مبيد حشري طبيعي.',
 7, -- DailyLife
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '13 days'),

-- صوصو stories (Chick 1)
('bbbb1111-1009-1009-1009-000000000009',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0005-0005-0005-000000000005',
 'صوصو يتعلم المشي! يتعثر أحياناً لكنه يقف مباشرة ويحاول مرة أخرى. إصرار جميل من هذا الصغير!',
 1, -- NewBeginning
 'https://upload.wikimedia.org/wikipedia/commons/5/53/Yellow_chick.jpg',
 NULL,
 false,
 NOW() - INTERVAL '6 days'),

('bbbb1111-1010-1010-1010-000000000010',
 'bbbbbbbb-1111-2222-3333-444444444444',
 'bbbb1111-0005-0005-0005-000000000005',
 'أطفال الجيران أحبوا صوصو! قضوا ساعة يلعبون معه ويطعمونه. سعادة الأطفال مع الحيوانات شيء جميل.',
 0, -- LoveAndBond
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '12 days'),

-- زاجل stories (Homing Pigeon)
('cccc1111-1001-1001-1001-000000000001',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0001-0001-0001-000000000001',
 'زاجل فاز في سباق اليوم! قطع مسافة 200 كيلومتر وعاد أولاً. فخور جداً بهذا البطل!',
 2, -- ProgressAndWins
 'https://upload.wikimedia.org/wikipedia/commons/6/6a/Racing_pigeons.jpg',
 NULL,
 false,
 NOW() - INTERVAL '3 days'),

('cccc1111-1002-1002-1002-000000000002',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0001-0001-0001-000000000001',
 'تدريب زاجل اليوم. أطلقته من مسافة 50 كيلومتر وعاد في أقل من ساعة. سرعته تتحسن باستمرار.',
 7, -- DailyLife
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '10 days'),

-- طيار stories (Flying Pigeon)
('cccc1111-1003-1003-1003-000000000003',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0002-0002-0002-000000000002',
 'طيار أدى اليوم حركات بهلوانية رائعة! دار في الهواء عدة مرات ثم هبط برشاقة. عرض رائع!',
 3, -- FunnyMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '5 days'),

('cccc1111-1004-1004-1004-000000000004',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0002-0002-0002-000000000002',
 'طيار يحب الطيران في وقت الغروب. ألوان السماء تجعل مشهده أجمل. لحظات سحرية كل مساء.',
 4, -- PeacefulMoment
 'https://upload.wikimedia.org/wikipedia/commons/5/5f/Pigeon_flying.jpg',
 NULL,
 false,
 NOW() - INTERVAL '15 days'),

-- نجمة stories (Collared Dove)
('cccc1111-1005-1005-1005-000000000005',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0003-0003-0003-000000000003',
 'نجمة تهدل بصوت جميل كل صباح. صوتها يجلب السلام للمكان. أستيقظ على صوتها بسعادة.',
 4, -- PeacefulMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '7 days'),

('cccc1111-1006-1006-1006-000000000006',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0003-0003-0003-000000000003',
 'نجمة جاءت وحطت على كتفي اليوم! أول مرة تفعل هذا. شعور رائع أن تثق بي لهذه الدرجة.',
 0, -- LoveAndBond
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '14 days'),

-- أمير stories (Muscovy Drake)
('cccc1111-1007-1007-1007-000000000007',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0004-0004-0004-000000000004',
 'أمير بدأ يغير ريشه! الريش الجديد أجمل من القديم. سيصبح أكثر وسامة بعد اكتمال التغيير.',
 6, -- CareAndHealth
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '8 days'),

('cccc1111-1008-1008-1008-000000000008',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0004-0004-0004-000000000004',
 'أمير يجلس بجانب البركة بهدوء. يراقب الماء والسماء. يبدو كأنه يتأمل. طائر حكيم!',
 4, -- PeacefulMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '18 days'),

-- لؤلؤة stories (Black Duck)
('cccc1111-1009-1009-1009-000000000009',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0005-0005-0005-000000000005',
 'لؤلؤة سبحت لأول مرة في البركة الكبيرة! كانت خائفة في البداية لكنها تشجعت وأصبحت ماهرة.',
 1, -- NewBeginning
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '4 days'),

('cccc1111-1010-1010-1010-000000000010',
 'cccccccc-1111-2222-3333-444444444444',
 'cccc1111-0005-0005-0005-000000000005',
 'ريش لؤلؤة الأسود يلمع تحت الشمس! جمال طبيعي رائع. كل من يراها يسأل عن نوعها.',
 7, -- DailyLife
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '11 days'),

-- معلمة stories (Educational Hen)
('dddd1111-1001-1001-1001-000000000001',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0001-0001-0001-000000000001',
 'درس اليوم عن دورة حياة الدجاج! الأطفال شاهدوا معلمة وهي ترعى صيصانها. تعلموا الكثير من المشاهدة المباشرة.',
 2, -- ProgressAndWins
 'https://upload.wikimedia.org/wikipedia/commons/7/75/Chicken_life_cycle.jpg',
 NULL,
 false,
 NOW() - INTERVAL '2 days'),

('dddd1111-1002-1002-1002-000000000002',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0001-0001-0001-000000000001',
 'الطلاب يحبون إطعام معلمة! يتنافسون على من يعطيها الحبوب. تعلمهم اللطف مع الحيوانات.',
 0, -- LoveAndBond
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '9 days'),

-- ورقاء stories (Dove)
('dddd1111-1003-1003-1003-000000000003',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0002-0002-0002-000000000002',
 'ورقاء بنت عشها في شجرة ساحة المدرسة! الطلاب يراقبونها كل يوم من النافذة. درس حي عن الطيور.',
 1, -- NewBeginning
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '6 days'),

('dddd1111-1004-1004-1004-000000000004',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0002-0002-0002-000000000002',
 'صباح هادئ مع صوت ورقاء. الطلاب يبدؤون يومهم بسماع هديلها الجميل. موسيقى طبيعية مجانية!',
 4, -- PeacefulMoment
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '16 days'),

-- حكيم stories (Canada Goose)
('dddd1111-1005-1005-1005-000000000005',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0003-0003-0003-000000000003',
 'حكيم وصل اليوم! السفر الطويل من الشمال انتهى. الطلاب فرحوا جداً برؤيته مرة أخرى هذا الشتاء.',
 1, -- NewBeginning
 'https://upload.wikimedia.org/wikipedia/commons/4/45/Canada_Goose_-_panoramio.jpg',
 NULL,
 false,
 NOW() - INTERVAL '1 day'),

('dddd1111-1006-1006-1006-000000000006',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0003-0003-0003-000000000003',
 'درسنا اليوم عن هجرة الطيور باستخدام حكيم كمثال. الطلاب تعلموا لماذا تهاجر بعض الطيور ومتى تعود.',
 2, -- ProgressAndWins
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '20 days'),

-- كبير stories (White Broiler)
('dddd1111-1007-1007-1007-000000000007',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0004-0004-0004-000000000004',
 'الطلاب يقيسون نمو كبير كل أسبوع! يسجلون الوزن والطول في دفتر خاص. تجربة علمية حقيقية.',
 2, -- ProgressAndWins
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '5 days'),

('dddd1111-1008-1008-1008-000000000008',
 'dddddddd-1111-2222-3333-444444444444',
 'dddd1111-0004-0004-0004-000000000004',
 'كبير يأكل من أيدي الأطفال بلطف! رغم حجمه الكبير، هو لطيف جداً. الأطفال لم يعودوا خائفين منه.',
 0, -- LoveAndBond
 NULL,
 NULL,
 false,
 NOW() - INTERVAL '12 days')
ON CONFLICT (story_id) DO NOTHING;

-- =============================================
-- LOVES for Arabic Birds
-- =============================================
INSERT INTO loves (user_id, bird_id, created_at) VALUES
-- Ahmed loves Fatima's birds
('aaaaaaaa-1111-2222-3333-444444444444', 'bbbb1111-0002-0002-0002-000000000002', NOW()), -- حمامة السلام
('aaaaaaaa-1111-2222-3333-444444444444', 'bbbb1111-0005-0005-0005-000000000005', NOW()), -- صوصو

-- Fatima loves Ahmed's and Omar's birds
('bbbbbbbb-1111-2222-3333-444444444444', 'aaaa1111-0003-0003-0003-000000000003', NOW()), -- وزوز
('bbbbbbbb-1111-2222-3333-444444444444', 'cccc1111-0001-0001-0001-000000000001', NOW()), -- زاجل

-- Omar loves everyone's birds
('cccccccc-1111-2222-3333-444444444444', 'aaaa1111-0001-0001-0001-000000000001', NOW()), -- بطوطة
('cccccccc-1111-2222-3333-444444444444', 'bbbb1111-0002-0002-0002-000000000002', NOW()), -- حمامة السلام
('cccccccc-1111-2222-3333-444444444444', 'dddd1111-0001-0001-0001-000000000001', NOW()), -- معلمة

-- Layla loves educational birds
('dddddddd-1111-2222-3333-444444444444', 'aaaa1111-0005-0005-0005-000000000005', NOW()), -- صياح
('dddddddd-1111-2222-3333-444444444444', 'bbbb1111-0005-0005-0005-000000000005', NOW()), -- صوصو
('dddddddd-1111-2222-3333-444444444444', 'cccc1111-0001-0001-0001-000000000001', NOW()) -- زاجل
ON CONFLICT (user_id, bird_id) DO NOTHING;

-- =============================================
-- BIRDS & STORIES for abdelfattahragab@outlook.com
-- =============================================
DO $$
DECLARE
    target_user_id UUID;
BEGIN
    -- Find the user
    SELECT user_id INTO target_user_id FROM users WHERE email = 'abdelfattahragab@outlook.com';

    IF target_user_id IS NOT NULL THEN
        -- Create 3 birds for this user
        INSERT INTO birds (
            bird_id, owner_id, name, species, tagline, description, image_url,
            created_at, loved_count, supported_count, donation_cents,
            is_premium, is_memorial, max_media_count
        ) VALUES
        -- Bird 1: White Pigeon
        ('ffff1111-0001-0001-0001-000000000001',
         target_user_id,
         'سلامة',
         'حمامة بيضاء',
         'رسول السلام',
         'سلامة حمامة بيضاء ناصعة تجلب البهجة للمنزل. تحب الطيران في الصباح الباكر وتعود دائماً.',
         'https://i.ibb.co/YT3T1k21/White-homing-pigeon.jpg',
         NOW() - INTERVAL '30 days',
         25, 8, 2500,
         false, false, 10),

        -- Bird 2: Rooster
        ('ffff1111-0002-0002-0002-000000000002',
         target_user_id,
         'فارس',
         'ديك بلدي',
         'صوت الفجر',
         'فارس ديك قوي بريش ملون جميل. يصيح كل صباح ليوقظ الجميع. حارس أمين للدجاج.',
         'https://i.ibb.co/cK3mKVqg/Lone-Rooster.jpg',
         NOW() - INTERVAL '45 days',
         32, 12, 3200,
         true, false, 20),

        -- Bird 3: Mandarin Duck
        ('ffff1111-0003-0003-0003-000000000003',
         target_user_id,
         'جميلة',
         'بطة ملونة',
         'لوحة فنية متحركة',
         'جميلة بطة ماندرين بألوان خلابة. تسبح برشاقة وتجذب الأنظار بجمالها الطبيعي.',
         'https://i.ibb.co/G4WygwGy/Mandarin-duck-arp.jpg',
         NOW() - INTERVAL '60 days',
         45, 18, 4500,
         true, false, 20)
        ON CONFLICT (bird_id) DO NOTHING;

        -- Create stories for the birds
        INSERT INTO stories (
            story_id, author_id, bird_id, content, mode, image_url, video_url, is_highlighted, created_at
        ) VALUES
        -- Stories for سلامة (White Pigeon)
        ('ffff1111-1001-1001-1001-000000000001',
         target_user_id,
         'ffff1111-0001-0001-0001-000000000001',
         'اليوم سلامة طارت لمسافة بعيدة وعادت بسلام! كنت قلقاً لكنها أثبتت أنها تعرف طريق العودة دائماً.',
         2, -- ProgressAndWins
         NULL, NULL, false,
         NOW() - INTERVAL '5 days'),

        ('ffff1111-1002-1002-1002-000000000002',
         target_user_id,
         'ffff1111-0001-0001-0001-000000000001',
         'سلامة باضت بيضتين! ننتظر الفقس بفارغ الصبر. إن شاء الله تكون فراخ صحية.',
         1, -- NewBeginning
         NULL, NULL, false,
         NOW() - INTERVAL '10 days'),

        -- Stories for فارس (Rooster)
        ('ffff1111-1003-1003-1003-000000000003',
         target_user_id,
         'ffff1111-0002-0002-0002-000000000002',
         'فارس اليوم دافع عن الدجاج ضد قط غريب! وقف بشجاعة ونفش ريشه حتى هرب القط. بطل حقيقي!',
         2, -- ProgressAndWins
         NULL, NULL, false,
         NOW() - INTERVAL '3 days'),

        ('ffff1111-1004-1004-1004-000000000004',
         target_user_id,
         'ffff1111-0002-0002-0002-000000000002',
         'صياح فارس هذا الصباح كان مختلفاً - أقوى وأجمل! يبدو أنه يتحسن مع الوقت.',
         7, -- DailyLife
         NULL, NULL, false,
         NOW() - INTERVAL '7 days'),

        -- Stories for جميلة (Mandarin Duck)
        ('ffff1111-1005-1005-1005-000000000005',
         target_user_id,
         'ffff1111-0003-0003-0003-000000000003',
         'جميلة سبحت اليوم مع البط الآخر لأول مرة! كانت خجولة في البداية لكنها تأقلمت بسرعة.',
         1, -- NewBeginning
         NULL, NULL, false,
         NOW() - INTERVAL '2 days'),

        ('ffff1111-1006-1006-1006-000000000006',
         target_user_id,
         'ffff1111-0003-0003-0003-000000000003',
         'ألوان جميلة تزداد جمالاً كل يوم! الريش الجديد يلمع تحت الشمس. سبحان الخالق!',
         4, -- PeacefulMoment
         NULL, NULL, false,
         NOW() - INTERVAL '14 days')
        ON CONFLICT (story_id) DO NOTHING;

        -- Add loves: this user loves some Arabic birds
        INSERT INTO loves (user_id, bird_id, created_at) VALUES
        (target_user_id, 'aaaa1111-0001-0001-0001-000000000001', NOW()), -- بطوطة
        (target_user_id, 'bbbb1111-0002-0002-0002-000000000002', NOW()), -- حمامة السلام
        (target_user_id, 'cccc1111-0001-0001-0001-000000000001', NOW())  -- زاجل
        ON CONFLICT (user_id, bird_id) DO NOTHING;

        -- Other users love this user's birds
        INSERT INTO loves (user_id, bird_id, created_at) VALUES
        ('aaaaaaaa-1111-2222-3333-444444444444', 'ffff1111-0001-0001-0001-000000000001', NOW()),
        ('bbbbbbbb-1111-2222-3333-444444444444', 'ffff1111-0002-0002-0002-000000000002', NOW()),
        ('cccccccc-1111-2222-3333-444444444444', 'ffff1111-0003-0003-0003-000000000003', NOW())
        ON CONFLICT (user_id, bird_id) DO NOTHING;

        RAISE NOTICE 'Successfully added birds and stories for abdelfattahragab@outlook.com';
    ELSE
        RAISE NOTICE 'User abdelfattahragab@outlook.com not found in database';
    END IF;
END $$;

COMMIT;

-- =============================================
-- Summary:
-- Users: 4 Arabic-speaking users
-- Birds: 20 birds (Duck x3, Drake x2, Goose x3, Hen x3, Rooster x1, Chicken x2, Broiler x2, Pigeon x5, Dove x1)
-- Stories: 40 stories (2 per bird)
--
-- For abdelfattahragab@outlook.com (if exists):
-- Birds: 3 (سلامة - حمامة بيضاء, فارس - ديك بلدي, جميلة - بطة ملونة)
-- Stories: 6 (2 per bird)
-- Loves: User loves 3 birds, and 3 users love user's birds
-- =============================================
