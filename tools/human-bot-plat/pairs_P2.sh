# The before/after clips of the story video (old v41 code in /root/work/old = a worktree of 8766b98 with test/ and tools/ copied in).
cd /root/work/gp; P=tools/human-bot-plat/pair.sh; O=/tmp/claude-0/pairs; R="#ff5a6e"; N="#5dffa0"
bash $P /root/work/gp $O/door_new.mp4 38935 39060 "بعد التصليح|الباب يدفّ الأصفر لبرا، ما يعلق داخله" $N TP=38930:2:380:631 HOLD=2:38930:39200 &
bash $P /root/work/old $O/page_old.mp4 6380 6760 "قبل التصليح (النسخة القديمة): تغيير الصفحة|رمشة بالغيوم، والأغنية تشتغل مرتين فوق بعض (اسمع)" $R &
wait
bash $P /root/work/gp $O/page_new.mp4 6380 6760 "بعد التصليح: تغيير الصفحة|الغيوم تطلع بنعومة، والأغنية مرة وحدة" $N &
bash $P /root/work/old $O/stack_old.mp4 7340 7620 "قبل التصليح (النسخة القديمة)|الأحمر ينط، والأزرق اللي على راسه يرتفع معه بدون ما يضغط شي" $R &
wait
bash $P /root/work/gp $O/stack_new.mp4 7340 7620 "بعد التصليح|الأحمر ينط، والأزرق يبقى مكانه" $N &
bash $P /root/work/old $O/fall_old.mp4 37860 38030 "قبل التصليح (تجربة: نرمي الأزرق تحت الخريطة)|يطيح برا الشاشة وما أحد يقدر يوصله، وبعد ٦ ثواني تعيد الصفحة" $R TP=37893:1:same:under &
wait
bash $P /root/work/gp $O/fall_new.mp4 37860 38030 "بعد التصليح (نفس التجربة)|يطيح مكان آخر أرض وقف عليها، والأصفر ينعشه" $N TP=37893:1:same:under &
bash $P /root/work/old $O/win_old.mp4 44440 44760 "قبل التصليح (النسخة القديمة)|آخر قطعة انحطت والأزرق طايح، وفازوا على طول" $R &
wait
bash $P /root/work/gp $O/win_new.mp4 44440 44800 "بعد التصليح|ما فيه فوز لين ينعشون الأزرق" $N
ls $O
