import 'package:flutter/material.dart';
import '../models/surah.dart';
import '../models/verse.dart';
import '../services/api_service.dart';

class SurahProvider with ChangeNotifier {
  final ApiService _apiService = ApiService();
  List<Surah> _surahs = [];
  List<Verse> _currentVerses = [];
  List<LLMModel> _models = [];
  Map<int, List<AIInterpretation>> _interpretations = {};
  bool _isLoading = false;
  bool _isSyncing = false;
  int? _currentUserId;
  String? _currentUsername;
  int _unreadCount = 0;
  bool _isLoggedIn = false;

  List<Surah> get surahs => _surahs;
  List<Verse> get currentVerses => _currentVerses;
  List<LLMModel> get models => _models;
  bool get isLoading => _isLoading;
  bool get isSyncing => _isSyncing;
  int? get currentUserId => _currentUserId;
  String? get currentUsername => _currentUsername;
  bool get isLoggedIn => _isLoggedIn;
  bool get isGuest => _currentUserId == 1 && _currentUsername == 'guest';
  int get unreadCount => _unreadCount;

  List<AIInterpretation> getInterpretations(int verseId) {
    return _interpretations[verseId] ?? [];
  }

  Future<void> initialize() async {
    await _setupModels();
    if (_isLoggedIn) {
      await fetchSurahs();
      await fetchUnreadCount();
    }
  }

  Future<void> _setupModels() async {
    try {
      _models = await _apiService.getModels();
      notifyListeners();
    } catch (e) {
      print('Error setting up models: $e');
    }
  }

  Future<void> fetchSurahs() async {
    _isLoading = true;
    notifyListeners();
    try {
      _surahs = await _apiService.getSurahs(userId: _currentUserId);
    } catch (e) {
      print('Provider Error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchUnreadCount() async {
    try {
      _unreadCount = await _apiService.getUnreadCount(userId: _currentUserId ?? 1);
      notifyListeners();
    } catch (e) {
      print('Error fetching unread count: $e');
    }
  }

  Future<List<Verse>> getVerses(int surahNumber) async {
    try {
      _currentVerses = await _apiService.getSurahVerses(surahNumber);
      notifyListeners();
      return _currentVerses;
    } catch (e) {
      print('Provider Error: $e');
      return [];
    }
  }

  Future<void> markSurahAsRead(int id) async {
    if (isGuest) return;
    try {
      await _apiService.markAsRead(id, userId: _currentUserId!);
      final index = _surahs.indexWhere((s) => s.id == id);
      if (index != -1) {
        _surahs[index].isRead = true;
        notifyListeners();
      }
      await fetchUnreadCount();
    } catch (e) {
      print('Provider Error: $e');
    }
  }

  Future<void> markVerseAsRead(int verseId) async {
    if (_currentUserId == null) {
      _currentUserId = 1; // Default user if not set
    }
    try {
      await _apiService.markVerseAsRead(verseId, userId: _currentUserId!);
      
      // Update local state immediately
      final index = _currentVerses.indexWhere((v) => v.id == verseId);
      if (index != -1) {
        _currentVerses[index].isRead = true;
        notifyListeners();
      }
      
      // Refresh unread count
      await fetchUnreadCount();
      print('Verse $verseId marked as read for user $_currentUserId');
    } catch (e) {
      print('Provider Error marking verse read: $e');
    }
  }

  Future<void> fetchInterpretations(int verseId) async {
    try {
      final interpretations = await _apiService.getVerseInterpretations(verseId);
      _interpretations[verseId] = interpretations;
      notifyListeners();
    } catch (e) {
      print('Error fetching interpretations: $e');
    }
  }

  Future<AIInterpretation?> generateInterpretation(int verseId, int modelId) async {
    try {
      final interpretation = await _apiService.generateInterpretation(verseId, modelId);
      if (interpretation != null) {
        _interpretations[verseId] = [...(_interpretations[verseId] ?? []), interpretation];
        notifyListeners();
      }
      return interpretation;
    } catch (e) {
      print('Error generating interpretation: $e');
      return null;
    }
  }

  Future<void> syncData() async {
    _isSyncing = true;
    notifyListeners();
    try {
      await _apiService.syncData();
      await fetchSurahs();
      await fetchUnreadCount();
    } catch (e) {
      print('Error syncing data: $e');
    } finally {
      _isSyncing = false;
      notifyListeners();
    }
  }

  Future<void> runBatchInterpretation(int modelId) async {
    _isLoading = true;
    notifyListeners();
    try {
      await _apiService.runBatchInterpretation(modelId);
      await fetchSurahs();
    } catch (e) {
      print('Error running batch interpretation: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> login(String username, String password) async {
    if (username == 'test' && password == 'test') {
      _currentUserId = 2; // Test user ID in DB
      _currentUsername = 'test';
      _isLoggedIn = true;
      notifyListeners();
      await fetchSurahs();
      await fetchUnreadCount();
    } else {
      throw Exception('Geçersiz kullanıcı adı veya şifre');
    }
  }

  void loginAsGuest() {
    _currentUserId = 1; // Default user
    _currentUsername = 'guest';
    _isLoggedIn = true;
    notifyListeners();
    fetchSurahs();
    fetchUnreadCount();
  }

  void logout() {
    _currentUserId = null;
    _currentUsername = null;
    _isLoggedIn = false;
    notifyListeners();
  }
}
