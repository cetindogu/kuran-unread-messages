import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/surah_provider.dart';
import '../models/surah.dart';
import 'verse_detail_screen.dart';

class SurahListScreen extends StatefulWidget {
  const SurahListScreen({super.key});

  @override
  State<SurahListScreen> createState() => _SurahListScreenState();
}

class _SurahListScreenState extends State<SurahListScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => Provider.of<SurahProvider>(context, listen: false).initialize());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F0E8),
      appBar: AppBar(
        title: const Text('Kuran-ı Kerim'),
        centerTitle: true,
        backgroundColor: const Color(0xFF1B5E20),
        foregroundColor: Colors.white,
        elevation: 0,
        actions: [
          Consumer<SurahProvider>(
            builder: (context, provider, _) {
              if (provider.unreadCount > 0) {
                return Padding(
                  padding: const EdgeInsets.only(right: 16),
                  child: Center(
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.red,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        '${provider.unreadCount}',
                        style: const TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  ),
                );
              }
              return const SizedBox.shrink();
            },
          ),
          Consumer<SurahProvider>(
            builder: (context, provider, _) {
              if (provider.isSyncing) {
                return const Padding(
                  padding: EdgeInsets.all(16),
                  child: SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      color: Colors.white,
                      strokeWidth: 2,
                    ),
                  ),
                );
              }
              return IconButton(
                icon: const Icon(Icons.sync),
                onPressed: () => _showSyncDialog(context, provider),
                tooltip: 'Verileri Senkronize Et',
              );
            },
          ),
          Consumer<SurahProvider>(
            builder: (context, provider, _) {
              return IconButton(
                icon: const Icon(Icons.batch_prediction),
                onPressed: () => _showBatchAIDialog(context, provider),
                tooltip: 'Toplu AI Yorumlama',
              );
            },
          ),
        ],
      ),
      body: Consumer<SurahProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  CircularProgressIndicator(color: Color(0xFF1B5E20)),
                  SizedBox(height: 16),
                  Text('Sureler yükleniyor...'),
                ],
              ),
            );
          }

          if (provider.surahs.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.book_outlined, size: 64, color: Colors.grey),
                  const SizedBox(height: 16),
                  const Text(
                    'Sureler bulunamadı',
                    style: TextStyle(fontSize: 18, color: Colors.grey),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Senkronizasyon yaparak verileri indirin',
                    style: TextStyle(color: Colors.grey),
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton.icon(
                    onPressed: () => _showSyncDialog(context, provider),
                    icon: const Icon(Icons.sync),
                    label: const Text('Senkronize Et'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF1B5E20),
                      foregroundColor: Colors.white,
                    ),
                  ),
                ],
              ),
            );
          }

          return ListView.builder(
            padding: const EdgeInsets.symmetric(vertical: 8),
            itemCount: provider.surahs.length,
            itemBuilder: (context, index) {
              final surah = provider.surahs[index];
              return _SurahCard(
                surah: surah,
                onTap: () => _showSurahDetail(context, surah),
                onMarkRead: provider.isGuest
                    ? null
                    : () => provider.markSurahAsRead(surah.id),
              );
            },
          );
        },
      ),
    );
  }

  void _showBatchAIDialog(BuildContext context, SurahProvider provider) {
    int? selectedModelId;
    if (provider.models.isNotEmpty) {
      selectedModelId = provider.models.first.id;
    }

    showDialog(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Toplu AI Yorumlama'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Tüm ayetleri seçilen model ile yorumlatmak ister misiniz? Bu işlem uzun sürebilir.'),
              const SizedBox(height: 16),
              DropdownButton<int>(
                value: selectedModelId,
                isExpanded: true,
                onChanged: (value) {
                  setDialogState(() {
                    selectedModelId = value;
                  });
                },
                items: provider.models.map((model) {
                  return DropdownMenuItem<int>(
                    value: model.id,
                    child: Text(model.displayName),
                  );
                }).toList(),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('İptal'),
            ),
            ElevatedButton(
              onPressed: selectedModelId == null
                  ? null
                  : () {
                      Navigator.pop(dialogContext);
                      provider.runBatchInterpretation(selectedModelId!);
                    },
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF6A4C93),
                foregroundColor: Colors.white,
              ),
              child: const Text('Başlat'),
            ),
          ],
        ),
      ),
    );
  }

  void _showSyncDialog(BuildContext context, SurahProvider provider) {
    showDialog(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Veri Senkronizasyonu'),
        content: const Text(
          'Kuran verilerini internetten indirerek veritabanını güncellemek istiyor musunuz? Bu işlem birkaç dakika sürebilir.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('İptal'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(dialogContext);
              provider.syncData();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFF1B5E20),
              foregroundColor: Colors.white,
            ),
            child: const Text('Senkronize Et'),
          ),
        ],
      ),
    );
  }

  void _showSurahDetail(BuildContext context, Surah surah) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => VerseDetailScreen(surah: surah),
      ),
    );
  }
}

class _SurahCard extends StatelessWidget {
  final Surah surah;
  final VoidCallback onTap;
  final VoidCallback? onMarkRead;

  const _SurahCard({
    required this.surah,
    required this.onTap,
    this.onMarkRead,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      elevation: surah.isRead ? 0 : 2,
      color: surah.isRead ? Colors.grey.shade100 : Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: surah.isRead
            ? BorderSide.none
            : const BorderSide(color: Color(0xFFE8E0D0)),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: surah.isRead
                      ? Colors.grey.shade300
                      : const Color(0xFF1B5E20),
                  borderRadius: BorderRadius.circular(24),
                ),
                child: Center(
                  child: Text(
                    '${surah.revelationOrder}',
                    style: TextStyle(
                      color: surah.isRead ? Colors.grey.shade700 : Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${surah.surahNumber}. ${surah.englishName}',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                        color: surah.isRead ? Colors.grey : Colors.black87,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      surah.meaning,
                      style: TextStyle(
                        fontSize: 13,
                        color: surah.isRead ? Colors.grey : Colors.grey.shade600,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(
                          Icons.menu_book,
                          size: 14,
                          color: Colors.grey.shade500,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${surah.verseCount} ayet',
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey.shade500,
                          ),
                        ),
                        if (surah.isRead) ...[
                          const SizedBox(width: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 2,
                            ),
                            decoration: BoxDecoration(
                              color: Colors.green.shade100,
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Text(
                              'Okundu',
                              style: TextStyle(
                                fontSize: 10,
                                color: Colors.green.shade700,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              if (onMarkRead != null && !surah.isRead)
                IconButton(
                  icon: const Icon(Icons.check_circle_outline),
                  color: const Color(0xFF1B5E20),
                  onPressed: onMarkRead,
                  tooltip: 'Okundu olarak işaretle',
                ),
            ],
          ),
        ),
      ),
    );
  }
}