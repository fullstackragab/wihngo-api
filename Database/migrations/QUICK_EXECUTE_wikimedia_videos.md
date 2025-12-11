# Quick Execute - Update Bird Videos (Wikimedia Commons)

## ?? Direct Video File URLs
All videos are direct `.webm` files from Wikimedia Commons - downloadable and embeddable.

## ?? Quick Execute

### PowerShell (Recommended)
```powershell
cd C:\.net\Wihngo
$env:PGPASSWORD='postgres'; psql -h localhost -U postgres -d wihngo -f Database/migrations/update_bird_videos_wikimedia.sql; $env:PGPASSWORD=$null
```

### Linux/Mac
```bash
cd /path/to/Wihngo
PGPASSWORD='postgres' psql -h localhost -U postgres -d wihngo -f Database/migrations/update_bird_videos_wikimedia.sql
```

### pgAdmin
1. Open pgAdmin
2. Connect to PostgreSQL server
3. Right-click **wihngo** database ? **Query Tool**
4. **File ? Open** ? Select `Database/migrations/update_bird_videos_wikimedia.sql`
5. Click **Execute** (?) or press **F5**

## ?? What Gets Updated

| Bird | Species | Video URL |
|------|---------|-----------|
| Sunny | Serinus canaria | `Canary_singing.webm` |
| Anna's Hummingbird | Calypte anna | `Calypte_anna_male.webm` |
| American Robin | Turdus migratorius | `Turdus_migratorius_video.webm` |
| Black-capped Chickadee | Poecile atricapillus | `Poecile_atricapillus.webm` |
| Anna's Juvenile | Calypte anna | `Calypte_anna_feeding.webm` |

## ? Expected Output

```
==============================================
VIDEO URL UPDATE COMPLETED!
==============================================

Birds updated with Wikimedia Commons video URLs: 5

Updated birds:
  ? American Robin (Turdus migratorius)
    https://upload.wikimedia.org/wikipedia/commons/6/61/Turdus_migratorius_video.webm
  ? Anna's Hummingbird (Calypte anna)
    https://upload.wikimedia.org/wikipedia/commons/3/3c/Calypte_anna_male.webm
  ? Anna's Juvenile (Calypte anna)
    https://upload.wikimedia.org/wikipedia/commons/8/89/Calypte_anna_feeding.webm
  ? Black-capped Chickadee (Poecile atricapillus)
    https://upload.wikimedia.org/wikipedia/commons/4/4f/Poecile_atricapillus.webm
  ? Sunny (Serinus canaria)
    https://upload.wikimedia.org/wikipedia/commons/5/53/Canary_singing.webm

All video URLs are direct .webm files from Wikimedia Commons
==============================================
```

## ?? Verify the Update

```sql
-- Check updated birds
SELECT bird_id, name, species, 
       LEFT(video_url, 60) || '...' as video_preview
FROM birds
WHERE bird_id IN (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003',
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001'
)
ORDER BY name;
```

## ?? Test Video URLs

All URLs are publicly accessible. You can test them directly:

```bash
# Download and test a video
curl -o test_canary.webm "https://upload.wikimedia.org/wikipedia/commons/5/53/Canary_singing.webm"

# Or open in browser (Chrome/Firefox support .webm)
```

## ?? If You Get Transaction Error

If you see: `current transaction is aborted, commands ignored until end of transaction block`

**Fix:**
```sql
ROLLBACK;
```

Then run the script again.

## ?? Notes

- ? All videos are direct file URLs (`.webm` format)
- ? Hosted on Wikimedia Commons (free, public domain)
- ? No cookies, tracking, or rate limits
- ? Perfect for embedding in `<video>` tags
- ? Species-accurate for each bird

## ?? HTML Video Tag Example

```html
<video controls width="640" height="480">
  <source src="https://upload.wikimedia.org/wikipedia/commons/5/53/Canary_singing.webm" type="video/webm">
  Your browser does not support the video tag.
</video>
```

---

**Ready to execute!** ??
