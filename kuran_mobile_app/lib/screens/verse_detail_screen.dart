import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/surah_provider.dart';
import '../models/surah.dart';
import '../models/verse.dart';

class VerseDetailScreen extends StatefulWidget {
  final Surah surah;

  const VerseDetailScreen({super.key, required this.surah});

  @override
  State<VerseDetailScreen> createState() => _VerseDetailScreenState();
}

class _VerseDetailScreenState extends State<VerseDetailScreen> {
  List<Verse> _verses = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadVerses();
  }

  Future<void> _loadVerses() async {
    final provider = Provider.of<SurahProvider>(context, listen: false);
    final verses = await provider.getVerses(widget.surah.surahNumber);
    setState(() {
      _verses = verses;
      _isLoading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F0E8),
      appBar: AppBar(
        title: Text('${widget.surah.surahNumber}. ${widget.surah.englishName}'),
        backgroundColor: const Color(0xFF1B5E20),
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      body: _isLoading
          ? const Center(
              child: CircularProgressIndicator(color: Color(0xFF1B5E20)),
            )
          : _verses.isEmpty
              ? const Center(
                  child: Text('Ayetler yüklenemedi'),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _verses.length,
                  itemBuilder: (context, index) {
                    return _VerseCard(
                      verse: _verses[index],
                      surahNumber: widget.surah.surahNumber,
                      onInterpretationGenerated: () {
                        setState(() {});
                      },
                    );
                  },
                ),
    );
  }
}

class _VerseCard extends StatefulWidget {
  final Verse verse;
  final int surahNumber;
  final VoidCallback onInterpretationGenerated;

  const _VerseCard({
    required this.verse,
    required this.surahNumber,
    required this.onInterpretationGenerated,
  });

  @override
  State<_VerseCard> createState() => _VerseCardState();
}

class _VerseCardState extends State<_VerseCard> {
  bool _showInterpretations = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<SurahProvider>(context, listen: false)
          .fetchInterpretations(widget.verse.id);
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = Provider.of<SurahProvider>(context);
    final interpretations = provider.getInterpretations(widget.verse.id);

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    color: const Color(0xFF1B5E20),
                    borderRadius: BorderRadius.circular(18),
                  ),
                  child: Center(
                    child: Text(
                      '${widget.verse.verseNumber}',
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 14,
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        widget.verse.arabicText,
                        style: const TextStyle(
                          fontSize: 24,
                          fontFamily: 'Amiri',
                          height: 1.5,
                          color: Color(0xFF1A1A1A),
                        ),
                        textAlign: TextAlign.right,
                      ),
                      const SizedBox(height: 12),
                      Text(
                        widget.verse.turkishTranslation,
                        style: const TextStyle(
                          fontSize: 15,
                          height: 1.6,
                          color: Color(0xFF333333),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () {
                      provider.markVerseAsRead(widget.verse.id);
                    },
                    icon: Icon(
                      widget.verse.isRead
                          ? Icons.check_circle
                          : Icons.check_circle_outline,
                      size: 18,
                    ),
                    label: Text(
                      widget.verse.isRead ? 'Okundu' : 'Okundu İşaretle',
                      style: const TextStyle(fontSize: 13),
                    ),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: const Color(0xFF1B5E20),
                      side: const BorderSide(color: Color(0xFF1B5E20)),
                      padding: const EdgeInsets.symmetric(vertical: 8),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () {
                      setState(() {
                        _showInterpretations = !_showInterpretations;
                      });
                    },
                    icon: Icon(
                      _showInterpretations
                          ? Icons.auto_stories
                          : Icons.auto_stories_outlined,
                      size: 18,
                    ),
                    label: Text(
                      _showInterpretations ? 'Yorumları Gizle' : 'AI Yorumları',
                      style: const TextStyle(fontSize: 13),
                    ),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: const Color(0xFF6A4C93),
                      side: const BorderSide(color: Color(0xFF6A4C93)),
                      padding: const EdgeInsets.symmetric(vertical: 8),
                    ),
                  ),
                ),
              ],
            ),
            if (_showInterpretations) ...[
              const SizedBox(height: 16),
              _InterpretationSection(
                verse: widget.verse,
                interpretations: interpretations,
                models: provider.models,
                onGenerate: (modelId) async {
                  await provider.generateInterpretation(widget.verse.id, modelId);
                  widget.onInterpretationGenerated();
                },
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _InterpretationSection extends StatefulWidget {
  final Verse verse;
  final List<AIInterpretation> interpretations;
  final List<LLMModel> models;
  final Function(int) onGenerate;

  const _InterpretationSection({
    required this.verse,
    required this.interpretations,
    required this.models,
    required this.onGenerate,
  });

  @override
  State<_InterpretationSection> createState() => _InterpretationSectionState();
}

class _InterpretationSectionState extends State<_InterpretationSection> {
  String? _selectedModelName;

  @override
  void initState() {
    super.initState();
    _syncSelectedModel();
  }

  @override
  void didUpdateWidget(covariant _InterpretationSection oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.interpretations != widget.interpretations) {
      _syncSelectedModel();
    }
  }

  void _syncSelectedModel() {
    if (widget.interpretations.isEmpty) {
      _selectedModelName = null;
      return;
    }

    final modelNames = widget.interpretations.map((i) => i.modelName).toSet().toList()..sort();
    if (_selectedModelName == null || !modelNames.contains(_selectedModelName)) {
      _selectedModelName = modelNames.first;
    }
  }

  @override
  Widget build(BuildContext context) {
    final existingModelNames = widget.interpretations.map((i) => i.modelName).toSet();
    final availableModelNames = widget.interpretations.map((i) => i.modelName).toSet().toList()..sort();

    AIInterpretation? selected;
    if (_selectedModelName != null) {
      final candidates = widget.interpretations.where((i) => i.modelName == _selectedModelName).toList()
        ..sort((a, b) => b.generatedAt.compareTo(a.generatedAt));
      if (candidates.isNotEmpty) {
        selected = candidates.first;
      }
    }

    final addableModels = widget.models.where((m) => !existingModelNames.contains(m.modelName)).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(vertical: 8),
          decoration: BoxDecoration(
            border: Border(
              bottom: BorderSide(color: Colors.grey.shade300),
            ),
          ),
          child: Row(
            children: [
              const Icon(
                Icons.psychology,
                color: Color(0xFF6A4C93),
                size: 20,
              ),
              const SizedBox(width: 8),
              const Text(
                'AI Yorumları',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF6A4C93),
                ),
              ),
              const Spacer(),
              Text(
                '${widget.interpretations.length} yorum',
                style: TextStyle(
                  fontSize: 12,
                  color: Colors.grey.shade600,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        if (widget.interpretations.isEmpty)
          _EmptyInterpretationCard(models: widget.models, onGenerate: widget.onGenerate)
        else ...[
          if (availableModelNames.length > 1)
            DropdownButton<String>(
              value: _selectedModelName,
              isExpanded: true,
              onChanged: (value) {
                setState(() {
                  _selectedModelName = value;
                });
              },
              items: availableModelNames
                  .map((name) => DropdownMenuItem<String>(
                        value: name,
                        child: Text(name),
                      ))
                  .toList(),
            ),
          if (selected != null) ...[
            const SizedBox(height: 8),
            _InterpretationCard(interpretation: selected),
          ],
          if (addableModels.isNotEmpty) ...[
            const SizedBox(height: 12),
            _AddMoreInterpretations(
              models: addableModels,
              onGenerate: widget.onGenerate,
            ),
          ],
        ],
      ],
    );
  }
}

class _EmptyInterpretationCard extends StatelessWidget {
  final List<LLMModel> models;
  final Function(int) onGenerate;

  const _EmptyInterpretationCard({
    required this.models,
    required this.onGenerate,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey.shade100,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey.shade300),
      ),
      child: Column(
        children: [
          Icon(
            Icons.lightbulb_outline,
            size: 48,
            color: Colors.amber.shade600,
          ),
          const SizedBox(height: 12),
          const Text(
            'Bu ayet için henüz AI yorumu bulunmuyor',
            style: TextStyle(
              fontSize: 14,
              color: Colors.grey,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 16),
          if (models.isEmpty)
            const Text(
              'Model bulunamadı',
              style: TextStyle(color: Colors.grey),
            ),
        ],
      ),
    );
  }
}

class _InterpretationCard extends StatelessWidget {
  final AIInterpretation interpretation;

  const _InterpretationCard({required this.interpretation});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFFAFAFA),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE0E0E0)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 8,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: const Color(0xFF6A4C93),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  interpretation.modelName,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
              const Spacer(),
              Text(
                _formatDate(interpretation.generatedAt),
                style: TextStyle(
                  fontSize: 10,
                  color: Colors.grey.shade600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            interpretation.interpretation,
            style: const TextStyle(
              fontSize: 14,
              height: 1.6,
              color: Color(0xFF333333),
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year}';
  }
}

class _AddMoreInterpretations extends StatelessWidget {
  final List<LLMModel> models;
  final Function(int) onGenerate;

  const _AddMoreInterpretations({
    required this.models,
    required this.onGenerate,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.amber.shade50,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.amber.shade200),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.add_circle_outline, color: Colors.amber.shade700, size: 20),
              const SizedBox(width: 8),
              Text(
                'Diğer Modellerden Yorum Ekle',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Colors.amber.shade800,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: models.map((model) {
              return InkWell(
                onTap: () => onGenerate(model.id),
                borderRadius: BorderRadius.circular(8),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.amber.shade300),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        Icons.play_arrow,
                        size: 14,
                        color: Colors.amber.shade700,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        model.displayName,
                        style: TextStyle(
                          fontSize: 11,
                          color: Colors.amber.shade800,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}
