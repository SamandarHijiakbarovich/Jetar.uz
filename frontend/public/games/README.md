# O'yin muqova rasmlari

Bu papkaga har o'yin uchun **bitta muqova (hero) rasmi** qo'yiladi. Rasm avtomatik
ravishda e'lon kartochkalari va e'lon sahifasida ko'rinadi. Fayl bo'lmasa — sayt
buzilmaydi, generativ (chizma) muqova ko'rsatiladi.

## Kutilayotgan fayllar

Fayl nomi o'yin `slug`iga mos bo'lishi shart (`src/lib/games.ts` bilan bir xil):

| O'yin | Fayl |
| --- | --- |
| eFootball | `efootball.jpg` |
| PUBG Mobile | `pubg-mobile.jpg` |
| Free Fire | `free-fire.jpg` |
| CS:GO | `csgo.jpg` |
| Dota 2 | `dota-2.jpg` |
| Valorant | `valorant.jpg` |
| COD Mobile | `cod-mobile.jpg` |

## Tavsiyalar

- Format: `.jpg` (yoki `.webp` — u holda `games.ts` dagi kengaytmani ham o'zgartiring).
- Nisbat: **16:9** (masalan 1280×720). Muqova shu nisbatda kesiladi.
- Hajm: iloji boricha ~200–400 KB (sahifa tez ochilishi uchun).

> ⚠️ **Mualliflik huquqi:** faqat **o'zingiz huquqiga ega bo'lgan** (litsenziyalangan
> yoki original) rasmlarni qo'ying. O'yin kompaniyalarining rasmiy san'ati va
> logotiplari himoyalangan bo'lishi mumkin.
