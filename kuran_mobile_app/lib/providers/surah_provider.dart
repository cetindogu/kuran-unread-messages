import 'package:flutter/material.dart';
import '../models/surah.dart';
import '../models/verse.dart';
import '../services/api_service.dart';

class SurahProvider with ChangeNotifier {
  final ApiService _apiService = ApiService();
  List<Surah> _surahs = [];
  bool _isLoading = false;
  int? _currentUserId;
  String? _currentUsername;

  List<Surah> get surahs => _surahs;
  bool get isLoading => _isLoading;
  int? get currentUserId => _currentUserId;
  String? get currentUsername => _currentUsername;
  bool get isGuest => _currentUserId == null;

  Future<void> login(String username, String password) async {
    if (username == 'test' && password == 'test') {
      _currentUserId = 2; // Fixed ID for test user
      _currentUsername = 'test';
      await fetchSurahs();
    } else {
      throw Exception('Geçersiz kullanıcı adı veya şifre');
    }
  }

  void logout() {
    _currentUserId = null;
    _currentUsername = null;
    fetchSurahs();
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

  Future<void> markSurahAsRead(int id) async {
    if (isGuest) return;
    try {
      await _apiService.markAsRead(id, userId: _currentUserId!);
      final index = _surahs.indexWhere((s) => s.id == id);
      if (index != -1) {
        _surahs[index].isRead = true;
        notifyListeners();
      }
    } catch (e) {
      print('Provider Error: $e');
    }
  }

  Future<List<Verse>> getVerses(int surahNumber) async {
    return await _apiService.getSurahVerses(surahNumber);
  }
}
