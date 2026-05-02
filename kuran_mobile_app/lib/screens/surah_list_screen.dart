import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/surah_provider.dart';
import '../models/surah.dart';
import '../models/verse.dart';
import 'login_screen.dart';

class SurahListScreen extends StatefulWidget {
  const SurahListScreen({super.key});

  @override
  State<SurahListScreen> createState() => _SurahListScreenState();
}

class _SurahListScreenState extends State<SurahListScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() =>
        Provider.of<SurahProvider>(context, listen: false).fetchSurahs());
  }

  void _showSurahDetail(Surah surah) async {
    final provider = Provider.of<SurahProvider>(context, listen: false);
    
    showDialog(
      context: context,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final verses = await provider.getVerses(surah.surahNumber);
      if (!mounted) return;
      Navigator.pop(context); // Close loading

      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        builder: (context) => DraggableScrollableSheet(
          initialChildSize: 0.9,
          minChildSize: 0.5,
          maxChildSize: 0.95,
          expand: false,
          builder: (context, scrollController) => Column(
            children: [
              Padding(
                padding: const EdgeInsets.all(16.0),
                child: Text(
                  '${surah.surahNumber}. ${surah.englishName} (${surah.name})',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              Expanded(
                child: ListView.builder(
                  controller: scrollController,
                  itemCount: verses.length,
                  itemBuilder: (context, index) {
                    final verse = verses[index];
                    return Padding(
                      padding: const EdgeInsets.all(8.0),
                      child: Card(
                        child: Padding(
                          padding: const EdgeInsets.all(12.0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text(
                                verse.arabicText,
                                style: const TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.bold,
                                  fontFamily: 'Amiri', // Optional: Add Arabic font
                                ),
                                textAlign: TextAlign.right,
                              ),
                              const SizedBox(height: 8),
                              Align(
                                alignment: Alignment.centerLeft,
                                child: Text(
                                  '${verse.verseNumber}. ${verse.turkishTranslation}',
                                  style: const TextStyle(fontSize: 16),
                                ),
                              ),
                              if (verse.summary.isNotEmpty) ...[
                                const Divider(),
                                Align(
                                  alignment: Alignment.centerLeft,
                                  child: Text(
                                    'Özet: ${verse.summary}',
                                    style: const TextStyle(
                                      fontStyle: FontStyle.italic,
                                      color: Colors.grey,
                                    ),
                                  ),
                                ),
                              ],
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Hata: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = Provider.of<SurahProvider>(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Nüzul Sırasına Göre Kuran'),
        elevation: 2,
        actions: [
          if (provider.isGuest)
            IconButton(
              icon: const Icon(Icons.login),
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => const LoginScreen()),
              ),
            )
          else
            Row(
              children: [
                Text(provider.currentUsername!),
                IconButton(
                  icon: const Icon(Icons.logout),
                  onPressed: () => provider.logout(),
                ),
              ],
            ),
        ],
      ),
      body: Consumer<SurahProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (provider.surahs.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Text('Sure bulunamadı.'),
                  ElevatedButton(
                    onPressed: () => provider.fetchSurahs(),
                    child: const Text('Yenile'),
                  ),
                ],
              ),
            );
          }

          return ListView.builder(
            itemCount: provider.surahs.length,
            itemBuilder: (context, index) {
              final surah = provider.surahs[index];
              return Card(
                margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                child: ListTile(
                  leading: CircleAvatar(
                    child: Text('${surah.revelationOrder}'),
                  ),
                  title: Text('${surah.surahNumber}. ${surah.englishName}'),
                  subtitle: Text(surah.meaning),
                  trailing: Checkbox(
                    value: surah.isRead,
                    onChanged: (provider.isGuest || surah.isRead)
                        ? null 
                        : (value) {
                            if (value == true) {
                              provider.markSurahAsRead(surah.id);
                            }
                          },
                  ),
                  onTap: () => _showSurahDetail(surah),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
