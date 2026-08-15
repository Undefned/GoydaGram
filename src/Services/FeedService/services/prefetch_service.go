package services

import (
	"context"
	"feed-service/models"
	"log"
	"sync"
	"time"
)

type PrefetchService struct {
	feedService  *FeedService
	prefetchChan chan prefetchJob
	wg           sync.WaitGroup
}

type prefetchJob struct {
	UserID   string
	Offset   int
	Limit    int
	Seen     []string
	ResultCh chan *models.FeedResponse
}

func NewPrefetchService(feedService *FeedService, workers int) *PrefetchService {
	ps := &PrefetchService{
		feedService:  feedService,
		prefetchChan: make(chan prefetchJob, 100),
	}

	for i := 0; i < workers; i++ {
		ps.wg.Add(1)
		go ps.worker()
	}

	return ps
}

func (ps *PrefetchService) worker() {
	defer ps.wg.Done()
	for job := range ps.prefetchChan {
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		result, err := ps.feedService.GetFeed(ctx, job.UserID, job.Offset, job.Limit, job.Seen)
		cancel()

		if err != nil {
			log.Printf("Prefetch error for user %s: %v", job.UserID, err)
			job.ResultCh <- nil
		} else {
			job.ResultCh <- result
		}
		close(job.ResultCh)
	}
}

func (ps *PrefetchService) Prefetch(userID string, offset, limit int, seen []string) <-chan *models.FeedResponse {
	ch := make(chan *models.FeedResponse, 1)
	job := prefetchJob{
		UserID:   userID,
		Offset:   offset,
		Limit:    limit,
		Seen:     seen,
		ResultCh: ch,
	}
	ps.prefetchChan <- job
	return ch
}

func (ps *PrefetchService) Close() {
	close(ps.prefetchChan)
	ps.wg.Wait()
}
